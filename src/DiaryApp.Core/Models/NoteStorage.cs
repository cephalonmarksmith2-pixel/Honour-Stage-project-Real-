namespace DiaryApp.Models;

public class NoteStorage
{
    private readonly string _folder;

    // The caller decides the folder. Tests use a temp one, the phone app uses its own.
    public NoteStorage(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(_folder); // does nothing if it already exists
    }

    // TODO: Save(Note note)
    //   - which file name? (think about invalid characters: Path.GetInvalidFileNameChars)
    //   - what text goes in the file? (you need the reverse of Parse)
    //   - File.WriteAllText(path, text)

    // TODO: List<Note> LoadAll()
    //   - Directory.GetFiles(_folder, "*.md")
    //   - for each file: File.ReadAllText, then NoteParser.Parse(Path.GetFileNameWithoutExtension(path), text)

    // TODO: Delete(string noteName)
}