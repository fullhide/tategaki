namespace TategakiPrint.Helpers;

public static class KanjiConverter
{
    private static readonly Dictionary<long, string> DaijiDigits = new()
    {
        { 1, "壱" }, { 2, "弐" }, { 3, "参" }, { 4, "四" }, { 5, "伍" },
        { 6, "六" }, { 7, "七" }, { 8, "八" }, { 9, "九" }
    };

    private static readonly Dictionary<long, string> NormalDigits = new()
    {
        { 1, "一" }, { 2, "二" }, { 3, "三" }, { 4, "四" }, { 5, "五" },
        { 6, "六" }, { 7, "七" }, { 8, "八" }, { 9, "九" }
    };

    public static string ToDaijiAmount(decimal amount)
    {
        long val = (long)amount;
        if (val == 0) return "金 零円";

        if (val < 10_000)
        {
            return $"金 {ConvertToNormalKanji(val)}円";
        }

        string result = "";

        long oku = val / 100_000_000;
        if (oku > 0)
        {
            result += ConvertUnder10000Daiji(oku) + "億";
            val %= 100_000_000;
        }

        long man = val / 10_000;
        if (man > 0)
        {
            result += ConvertUnder10000Daiji(man) + "萬";
            val %= 10_000;
        }

        if (val > 0)
        {
            result += ConvertUnder10000Daiji(val);
        }

        return $"金 {result}円";
    }

    private static string ConvertUnder10000Daiji(long val)
    {
        string str = "";
        long sen = val / 1000;
        if (sen > 0)
        {
            str += (sen == 1 ? "" : DaijiDigits[sen]) + "千";
            val %= 1000;
        }

        long hyaku = val / 100;
        if (hyaku > 0)
        {
            str += (hyaku == 1 ? "" : DaijiDigits[hyaku]) + "百";
            val %= 100;
        }

        long ju = val / 10;
        if (ju > 0)
        {
            str += (ju == 1 ? "" : DaijiDigits[ju]) + "拾";
            val %= 10;
        }

        if (val > 0)
        {
            str += DaijiDigits[val];
        }

        return str;
    }

    private static string ConvertToNormalKanji(long val)
    {
        string str = "";
        long sen = val / 1000;
        if (sen > 0)
        {
            str += (sen == 1 ? "" : NormalDigits[sen]) + "千";
            val %= 1000;
        }

        long hyaku = val / 100;
        if (hyaku > 0)
        {
            str += (hyaku == 1 ? "" : NormalDigits[hyaku]) + "百";
            val %= 100;
        }

        long ju = val / 10;
        if (ju > 0)
        {
            str += (ju == 1 ? "" : NormalDigits[ju]) + "十";
            val %= 10;
        }

        if (val > 0)
        {
            str += NormalDigits[val];
        }

        return str;
    }
}
