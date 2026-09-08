using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChessGui;

public class NetworkManager
{
    private TcpListener? listener;
    private TcpClient? client;
    private NetworkStream? stream;

    private bool isRunning;

    public bool IsConnected
    {
        get
        {
            return client != null && client.Connected;
        }
    }

    // MainForm יוכל להירשם לאירוע הזה
    public event Action<string>? MessageReceived;

    // אירוע כאשר נוצר חיבור
    public event Action? Connected;

    // אירוע כאשר החיבור נסגר או נופל
    public event Action? Disconnected;

    public async Task StartServerAsync(int port)
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            isRunning = true;

            Console.WriteLine($"Server started on port {port}");
            Console.WriteLine("Waiting for client...");

            client = await listener.AcceptTcpClientAsync();
            stream = client.GetStream();

            Console.WriteLine("Client connected!");

            Connected?.Invoke();

            _ = ReceiveMessagesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
            Disconnect();
        }
    }

    public async Task ConnectAsync(string ipAddress, int port)
    {
        try
        {
            client = new TcpClient();

            Console.WriteLine($"Connecting to {ipAddress}:{port}...");

            await client.ConnectAsync(ipAddress, port);

            stream = client.GetStream();
            isRunning = true;

            Console.WriteLine("Connected to server!");

            Connected?.Invoke();

            _ = ReceiveMessagesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
            Disconnect();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (!IsConnected || stream == null)
        {
            Console.WriteLine("Cannot send: no active connection.");
            return;
        }

        try
        {
            // \n מסמן סוף הודעה
            string messageWithEnding = message + "\n";

            byte[] data = Encoding.UTF8.GetBytes(messageWithEnding);

            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            Console.WriteLine($"Sent: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send error: {ex.Message}");
            Disconnect();
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        if (stream == null)
            return;

        byte[] buffer = new byte[1024];
        StringBuilder receivedData = new StringBuilder();

        try
        {
            while (isRunning && IsConnected)
            {
                int bytesRead = await stream.ReadAsync(
                    buffer,
                    0,
                    buffer.Length
                );

                // 0 אומר שהצד השני סגר את החיבור
                if (bytesRead == 0)
                    break;

                string text = Encoding.UTF8.GetString(
                    buffer,
                    0,
                    bytesRead
                );

                receivedData.Append(text);

                // TCP הוא Stream ולכן הודעה יכולה להגיע בחלקים
                // אנחנו מפרידים הודעות לפי \n
                while (receivedData.ToString().Contains('\n'))
                {
                    string allData = receivedData.ToString();

                    int newlineIndex = allData.IndexOf('\n');

                    string message = allData
                        .Substring(0, newlineIndex)
                        .Trim();

                    receivedData.Clear();

                    receivedData.Append(
                        allData.Substring(newlineIndex + 1)
                    );

                    if (message.Length > 0)
                    {
                        Console.WriteLine($"Received: {message}");
                        MessageReceived?.Invoke(message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (isRunning)
            {
                Console.WriteLine(
                    $"Receive error: {ex.Message}"
                );
            }
        }
        finally
        {
            Disconnect();
        }
    }

    public void Disconnect()
    {
        bool wasConnected = IsConnected || isRunning;

        isRunning = false;

        try
        {
            stream?.Close();
            client?.Close();
            listener?.Stop();
        }
        catch
        {
            // בזמן סגירה אין צורך להפיל את התוכנית
        }

        stream = null;
        client = null;
        listener = null;

        if (wasConnected)
        {
            Console.WriteLine("Disconnected.");
            Disconnected?.Invoke();
        }
    }
}