using DiaryApp.Models;
public class NoteLinkTests
{
    [Fact]
    public void ParseLink_PlainTitle_HasNoDisplay()
    {
        var parts = NoteLinks.ParseLink("Run 5");
        Assert.Equal("Run 5", parts.Title);
    }

    [Fact]
    public void ParseLink_WithAlias_SplitsOnPipe()
    {
        var parts = NoteLinks.ParseLink("[[Run 5| my 5k]]");

        Assert.Equal("Run 5", parts.Title);    // expected value FIRST, actual second
        Assert.Equal("my 5k", parts.Display);
    }

    [Fact]
    public void ParseLink_ExtraSpaces_AreTrimmed()
    {
        var parts = NoteLinks.ParseLink("[[  Run 5  |   my 5k   ]]");
        Assert.Equal("Run 5", parts.Title);    // expected value FIRST, actual second
        Assert.Equal("my 5k", parts.Display);
    }

    [Fact]
    public void Resolve_IgnoresCapitals()
    {
        var notes = new List<Note> { new Note { Title = "Run 5" } };

        var found = NoteLinks.Resolve("  run 5 ", notes);

        Assert.NotNull(found);
        Assert.Equal("Run 5", found!.Title);   // the ! says "I know it isn't null here"        
    }

    [Fact]
    public void Resolve_MissingNote_ReturnsNull()
    {
        /* "Swim" */
        var notes = new List<Note> { new Note { Title = "Run 5" } };

        var found = NoteLinks.Resolve("  Swim ", notes);

        Assert.Null(found);
    }

    // Makes a throwaway folder so tests never touch real notes.
    private static (NoteStorage storage, string folder) MakeStorage()
    {
        string folder = Path.Combine(Path.GetTempPath(), "diary-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        return (new NoteStorage(folder), folder);
    }

    [Fact]
    public void OpenOrCreate_Missing_CreatesAndReportsCreated()
    {
        var (storage, folder) = MakeStorage();

        var (note, created) = storage.OpenOrCreate("  Swim ");

        Assert.True(created);
        Assert.Equal("Swim", note.Title);          // trimmed
        Assert.NotNull(storage.Load("Swim"));      // the file really exists now

        Directory.Delete(folder, true);            // tidy up
    }

    [Fact]
    public void OpenOrCreate_Existing_DoesNotCreate()
    {
        var (storage, folder) = MakeStorage();
        storage.Save(new Note { Title = "Run 5" });
        int filesBefore = Directory.GetFiles(folder, "*.md").Length;

        var (note, created) = storage.OpenOrCreate("run 5");   // lower case on purpose

        Assert.False(created);
        Assert.Equal("Run 5", note.Title);                     // the real note, not what was typed
        Assert.Equal(filesBefore, Directory.GetFiles(folder, "*.md").Length);  // nothing new

        Directory.Delete(folder, true);
    }
}