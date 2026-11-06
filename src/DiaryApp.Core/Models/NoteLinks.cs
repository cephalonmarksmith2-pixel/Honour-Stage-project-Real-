using DiaryApp.Models;             // NoteStorage, Note, Mood, NoteWriter, FileNames
public static class NoteLinks
{
    // Holds the two halves of a link: [[Title|display text]]
    public record LinkParts(string Title, string? Display);

    public static LinkParts ParseLink(string linkText)
    {
        // This method strips the [[ and ]], so callers pass the link exactly as written.
        // Trim('[', ']') removes brackets from both ends only, and Trim() then
        // removes any spaces left at the edges, e.g. "[[ Run 5 ]]" -> "Run 5".
        string cleaned = (linkText ?? "").Trim('[', ']').Trim();

        // Alias case: "Run 5 | my 5k"
        if (cleaned.Contains('|'))
        {
            // The 2 means "at most two pieces", so "a|b|c" gives "a" and "b|c".
            // TrimEntries removes the spaces around each piece.
            string[] parts = cleaned.Split('|', 2, StringSplitOptions.TrimEntries);

            // An empty alias such as "[[Run 5|]]" counts as "no alias".
            string? display = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1];

            return new LinkParts(parts[0], display);
        }

        // Plain case: no pipe, so the whole thing is the title and there is no alias.
        return new LinkParts(cleaned, null);
    }

    // Returns the matching note, or null if none exists.
    public static Note? Resolve(string title, List<Note> notes)
    {
        // Trim the searched-for title; (title ?? "") stops a null crashing us.
        string wanted = (title ?? "").Trim();

        // FirstOrDefault checks each note in turn (n stands for the current note)
        // and returns the first match, or null if nothing matches.
        // OrdinalIgnoreCase makes "run 5" match "Run 5".
        return notes.FirstOrDefault(n =>
            string.Equals(n.Title.Trim(), wanted, StringComparison.OrdinalIgnoreCase));
    }
}