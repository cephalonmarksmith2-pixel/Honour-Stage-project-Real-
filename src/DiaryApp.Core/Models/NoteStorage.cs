namespace DiaryApp.Models;

public class NoteStorage
{
    private readonly string _folder;

    // The caller supplies the folder: tests pass a temp one, the phone app passes its private one
    public NoteStorage(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(_folder);   // does nothing if it already exists
    }

    // The ONE place a title turns into a path. Saving and link lookups both use it,
    // so [[Hill route]] and Hill-route.md always agree.
    private string PathFor(string title) =>
        Path.Combine(_folder, FileNames.MakeSafe(title) + ".md");

    public void Save(Note note)
    {
        // TODO (your decision): this overwrites silently. If "my note" and "my-note" map to the same
        // file, the second would replace the first. How would you detect that? (Hint: load the
        // existing file and compare titles.)
        File.WriteAllText(PathFor(note.Title), NoteWriter.ToFileText(note));
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

    // If a file has no [title:] tag, Parse falls back to the file name we pass in here
    private static Note ReadFile(string path) =>
        NoteParser.Parse(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path));
}