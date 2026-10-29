using System.Globalization;
using System.Text;

namespace DiaryApp.Models;

public static class NoteWriter
{
    public static string ToFileText(Note note)
    {
        var sb = new StringBuilder();

        // Tags first, in one line, in the same [key:value] format that Parse reads
        sb.Append($"[title:{Clean(note.Title)}]");
        sb.Append($"[exercise:{Clean(note.Exercise)}]");
        // InvariantCulture so the date is always yyyy-MM-dd, whatever the phone's language
        sb.Append($"[date:{note.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}]");
        sb.Append($"[mood:{note.Mood}]");
        sb.AppendLine();

        // Then the note's own text, which already contains any [[links]]
        sb.Append(note.NoteText ?? "");
        return sb.ToString();
    }

    // A "]" inside a value would end the tag early and break the parser, so swap brackets for
    // parentheses (and flatten new lines). This is a design choice you can change.
    private static string Clean(string value) =>
        value.Replace('[', '(').Replace(']', ')').Replace("\n", " ");
}