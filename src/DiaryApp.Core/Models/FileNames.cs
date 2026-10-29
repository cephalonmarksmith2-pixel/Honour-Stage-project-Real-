using System.Text;
using System.Text.RegularExpressions;

namespace DiaryApp.Models;

public static class FileNames
{
    // Characters Windows forbids, plus "/" for every system. Fixed, so it behaves the same everywhere.
    private static readonly char[] Forbidden = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

    public static string MakeSafe(string title)
    {
        var sb = new StringBuilder();
        foreach (char c in title.Trim())
        {
            if (c == ' ') sb.Append('-');                          // your rule: spaces become dashes
            else if (Array.IndexOf(Forbidden, c) >= 0 || char.IsControl(c))
                continue;                                          // drop forbidden characters
            else sb.Append(c);
        }

        // "a  b" would be "a--b", so collapse repeated dashes and trim them from the ends
        string result = Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');

        return result.Length == 0 ? "untitled" : result;           // never return an empty name
    }
}