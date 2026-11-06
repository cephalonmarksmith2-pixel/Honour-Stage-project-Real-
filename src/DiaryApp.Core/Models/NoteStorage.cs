namespace DiaryApp.Models;

/// <summary>
/// Reads and writes notes as .md files in one folder.
/// Unsaved edits are kept as ".draft" files next to the real note, so a crash or a dead battery
/// never loses what the user typed.
/// </summary>
public class NoteStorage
{
    private readonly string _folder;

    // The caller decides the folder: tests use a temp one, the phone app uses its private one
    public NoteStorage(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(_folder);   // does nothing if it already exists
    }

    // ---------- Paths ----------

    // The ONE place a title turns into a path. Saving and link lookups both use it,
    // so [[Hill route]] and Hill-route.md always agree.
    private string PathFor(string title) =>
        Path.Combine(_folder, FileNames.MakeSafe(title) + ".md");

    // A draft sits beside the real file with ".draft" added: "Hill-route.md.draft".
    // LoadAll only looks for "*.md", so drafts never appear in the list of notes.
    private string DraftPathFor(string title) => PathFor(title) + ".draft";

    // ---------- Safe writing ----------

    // Writes to a temporary file first, then swaps it into place in one step.
    // If the battery dies mid-write, the old file is still intact (only a stray .tmp is left behind).
    private static void WriteAtomically(string path, string text)
    {
        string temp = path + ".tmp";
        File.WriteAllText(temp, text);
        File.Move(temp, path, overwrite: true);
    }

    // ---------- Real notes ----------

    public void Save(Note note)
    {
        // TODO this overwrites silently. If "my note" and "my-note" map to the same file,
        // the second would replace the first. How would i detect that?
        WriteAtomically(PathFor(note.Title), NoteWriter.ToFileText(note));
    }

    // Finds a note by its title, or returns null if there isn't one
    public Note? Load(string title)
    {
        string path = PathFor(title);
        return File.Exists(path) ? ReadFile(path) : null;
    }

    public List<Note> LoadAll()
    {
        var notes = new List<Note>();
        foreach (string path in Directory.GetFiles(_folder, "*.md"))
            notes.Add(ReadFile(path));
        return notes;
    }

    public bool Delete(string title)
    {
        string path = PathFor(title);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

// ---------- Creating notes ----------

// Returns baseTitle if no note has that file name yet; otherwise "baseTitle 2", "baseTitle 3", ...
public string UniqueTitle(string baseTitle)
{
    // A blank base title would produce a confusing file name, so I used a sensible default
    if (string.IsNullOrWhiteSpace(baseTitle))
        baseTitle = "Untitled";

    string candidate = baseTitle;
    int counter = 2;

    // PathFor uses MakeSafe, so "my note" and "my-note" count as the same file, which is what I want
    while (File.Exists(PathFor(candidate)))
    {
        candidate = $"{baseTitle} {counter}";   // try the next number
        counter++;                               
    }

    return candidate;
}

// Makes a new, empty note with a unique title, SAVES it, and returns it.
// Mood and date start at the Note defaults (neutral, today).
public Note CreateNew(string baseTitle = "Untitled", string exercise = "Unassigned")
{
    var note = new Note
    {
        Title = UniqueTitle(baseTitle),
        Exercise = exercise,
        NoteText = ""
    };

    Save(note);
    return note;
}

// Returns the note, and whether it had to be created.
public (Note note, bool created) OpenOrCreate(string title)
{
    // Trim once and reuse it, so "  Swim " becomes "Swim" everywhere below.
    string trimmed = (title ?? "").Trim();

    // Look through every saved note for a match (ignores capitals).
    var localNotes = LoadAll();
    var existing = NoteLinks.Resolve(trimmed, localNotes);

    // Found it: hand it back, and say nothing was created.
    if (existing is not null)
        return (existing, false);

    // Not found: create it. CreateNew picks a free name and saves the file.
    var newNote = CreateNew(trimmed);
    return (newNote, true);
}

    // ---------- Drafts ----------

    // Saves unsaved edits. The draft is filed under the title the note had when editing STARTED
    // (forTitle), even if the user has since typed a new title. The new title is stored inside it.
    // This never touches the real note.
    public void SaveDraft(string forTitle, Note note) =>
        WriteAtomically(DraftPathFor(forTitle), NoteWriter.ToFileText(note));

    public bool HasDraft(string title) => File.Exists(DraftPathFor(title));

    // Returns the draft, or null if there isn't one
    public Note? LoadDraft(string title)
    {
        string path = DraftPathFor(title);
        return File.Exists(path) ? NoteParser.Parse(title, File.ReadAllText(path)) : null;
    }

    // When the draft was last written (null if there is no draft), for showing to the user
    public DateTime? DraftSavedAt(string title)
    {
        string path = DraftPathFor(title);
        return File.Exists(path) ? File.GetLastWriteTime(path) : null;
    }

    public void DeleteDraft(string title)
    {
        // File.Delete does nothing if the file doesn't exist, so no check is needed
        File.Delete(DraftPathFor(title));
    }

    // ---------- Helpers ----------

    // If a file has no [title:] tag, Parse falls back to the file name we pass in here
    private static Note ReadFile(string path) =>
        NoteParser.Parse(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path));
}