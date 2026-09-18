using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessGui;

public class NetworkManager
{
    private TcpListener? listener;
    private TcpClient? client;
    private NetworkStream? stream;

    private CancellationTokenSource? discoveryCts;
    private const int DiscoveryPort = 5001;
    private const string DiscoveryRequest = "CHESS_SERVER_DISCOVERY_REQ";
    private const string DiscoveryResponse = "CHESS_SERVER_DISCOVERY_RES";

    private bool isRunning;

    public bool IsConnected
    {
        get
        {
            return client != null && client.Connected;
        }
    }

    public event Action<string>? MessageReceived;
    public event Action? Connected;
    public event Action? Disconnected;

    // --- AUTOMATIC SERVER DISCOVERY METHOD ---
    public async Task<string?> DiscoverServerIpAsync(int timeoutMs = 3000)
    {
        using UdpClient udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        byte[] requestBytes = Encoding.UTF8.GetBytes(DiscoveryRequest);
        IPEndPoint broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);

        try
        {
            // Send broadcast request across the LAN
            await udpClient.SendAsync(requestBytes, requestBytes.Length, broadcastEndpoint);

            using var cts = new CancellationTokenSource(timeoutMs);

            // Wait for response from host
            UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token);
            string response = Encoding.UTF8.GetString(result.Buffer);

            if (response == DiscoveryResponse)
            {
                return result.RemoteEndPoint.Address.ToString();
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached, host not found
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Discovery error: {ex.Message}");
        }

        return null;
    }

    // --- UDP BEACON LISTENER FOR SERVER ---
    private void StartDiscoveryBeacon()
    {
        discoveryCts = new CancellationTokenSource();
        CancellationToken token = discoveryCts.Token;

        Task.Run(async () =>
        {
            using UdpClient udpServer = new UdpClient();
            udpServer.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpServer.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

            byte[] responseBytes = Encoding.UTF8.GetBytes(DiscoveryResponse);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await udpServer.ReceiveAsync(token);
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    if (message == DiscoveryRequest)
                    {
                        await udpServer.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Beacon error: {ex.Message}");
                }
            }
        }, token);
    }

    public async Task StartServerAsync(int port)
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            isRunning = true;

            // Start listening for UDP client discovery requests
            StartDiscoveryBeacon();

            Console.WriteLine($"Server started on port {port}");
            Console.WriteLine("Waiting for client...");

            client = await listener.AcceptTcpClientAsync();
            
            // Stop discovery beacon once a client successfully connects
            StopDiscoveryBeacon();

            stream = client.GetStream();

            Console.WriteLine("Client connected!");

            Connected?.Invoke();

            _ = ReceiveMessagesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Server error"
            );

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
            MessageBox.Show(
                ex.Message,
                "Connection error"
            );

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

                if (bytesRead == 0)
                    break;

                string text = Encoding.UTF8.GetString(
                    buffer,
                    0,
                    bytesRead
                );

                receivedData.Append(text);

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

    private void StopDiscoveryBeacon()
    {
        discoveryCts?.Cancel();
        discoveryCts?.Dispose();
        discoveryCts = null;
    }

    public void Disconnect()
    {
        bool wasConnected = IsConnected || isRunning;

        isRunning = false;
        StopDiscoveryBeacon();

        try
        {
            stream?.Close();
            client?.Close();
            listener?.Stop();
        }
        catch
        {
            // Ignore clean-up exceptions
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