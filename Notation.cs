static class Notation
{
    static public int ToRow(char number)
    {
        return 8 - (number - '0');
    }
    static public int ToCol(char letter)
    {
        return letter - 'a';
    }
}