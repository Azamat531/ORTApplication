public static class Letters
{
    // 0..4 -> А..Д и обратно, если понадобится
    public static int ToIndex(string letter)
    {
        if (string.IsNullOrEmpty(letter)) return -1;
        switch (letter.Trim().ToUpper())
        {
            case "А": return 0;
            case "Б": return 1;
            case "В": return 2;
            case "Г": return 3;
            case "Д": return 4;
            default: return -1;
        }
    }
}
