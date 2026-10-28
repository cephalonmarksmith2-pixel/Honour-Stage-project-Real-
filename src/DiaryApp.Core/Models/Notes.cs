using System.Text.RegularExpressions;

namespace DiaryApp.Models;

// Decide the mood choices yourself. Your graph has "happy", "sad" and "in between" zones.
public enum Mood
{
    // TODO: fill in the values
    Happy,
    Sad,
    Neutral,
    
    
}

/// <summary>
/// Base Inforamtion for what notes should do
/// </summary>
public class Note
{
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public List<string> Links { get; set; } = new();
    public string NoteName { get; set; } 
    public string? NoteText { get; set; }
    public Mood Mood { get; set; } = Mood.Neutral;
    public string? NoteLink { get; set; }
    public string Exercise { get; set; } = "Unassigned";
    
    // TODO: what uniquely identifies one note? a note is made by a user as a txt file 
    // TODO: what do you need to SEARCH by? (title, date...) title date and mood
    // TODO: what do you need to SHOW? (the text itself...) text links and the mood 
    // TODO: what does the graph need to GROUP by? (mood, exercise category...) can be chosen by the user but has to be atleast mood exercise and date
}


public class NoteSearch
{
    public List<Note> Notes { get; set; }
    
    static void FindNotes(List<Note> notes)
    {
        foreach (var note in notes)
        {
            
        }
    }
}


public static class NoteParser
{
    // Matches [key:value] but NOT [[links]]. Group 1 = key, Group 2 = value.
    // (?<!\[) means "not preceded by [", (?!\]) means "not followed by ]"
    private static readonly Regex PropertyPattern =
        new(@"(?<!\[)\[(\w+):([^\]]+)\](?!\])");

    // Matches [[Note name]]. Group 1 = the name inside the brackets.
    private static readonly Regex LinkPattern = new(@"\[\[(.+?)\]\]");

    public static Note Parse(string noteName, string rawText)
    {
        var note = new Note { NoteName = noteName };

        // Step 1: read every [key:value] tag
        foreach (Match m in PropertyPattern.Matches(rawText))
        {
            string key = m.Groups[1].Value.ToLowerInvariant();
            string value = m.Groups[2].Value.Trim();

            switch (key)
            {
                case "mood":
                    // TryParse returns true on success and fills 'mood'. On failure, keep the default.
                    if (Enum.TryParse(value, true, out Mood mood))
                        note.Mood = mood;
                    break;

                case "date":
                    // TODO: same pattern using DateOnly.TryParse, assigning note.Date
                    if (DateOnly.TryParse(value,out DateOnly date))
                        note.Date = date;
                    
                    break;

                case "exercise":
                    note.Exercise = value.ToLowerInvariant();
                    break;

                // Anything else is an unknown tag. What should happen? Ignore it, or keep it? Your call.
            }
        }

        // Step 2: collect the [[links]]
        foreach (Match m in LinkPattern.Matches(rawText))
            note.Links.Add(m.Groups[1].Value.Trim());
        note.NoteText = PropertyPattern.Replace(rawText, "").Trim();
        // TODO: strip the [tags] out of the text so NoteText is only what the user wrote
        // TODO: no exercise tag found? Leave Exercise as "Unassigned"

        return note;
    }
}
