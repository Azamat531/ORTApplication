public static class Letters
{
    // 0..4 -> �..� � �������, ���� �����������
    public static int ToIndex(string letter)
    {
        if (string.IsNullOrEmpty(letter)) return -1;
        switch (letter.Trim().ToUpper())
        {
            case "�": return 0;
            case "�": return 1;
            case "�": return 2;
            case "�": return 3;
            case "�": return 4;
            default: return -1;
        }
    }
}
