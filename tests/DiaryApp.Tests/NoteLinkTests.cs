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
    
    // UpdateLinksAfterRename tests (use a temp-folder storage, as before)
    [Fact]
    public void Rename_KeepsAlias()
    {
        var (storage, folder) = MakeStorage();   // the temp-folder helper from before

        // A note that links to "Run 5" with an alias.
        storage.Save(new Note { Title = "Diary", NoteText = "Went for a [[Run 5|my 5k]] today" });

        int changed = storage.UpdateLinksAfterRename("Run 5", "Run 6");

        Assert.Equal(1, changed);   // exactly one note was rewritten

        // Expected first, actual second. The alias "my 5k" must survive.
        Assert.Equal("Went for a [[Run 6|my 5k]] today", storage.Load("Diary")!.NoteText);

        Directory.Delete(folder, true);
    }

[Fact]
public void Rename_IgnoresCapitalsAndSpaces()
{
    var (storage, folder) = MakeStorage();
    storage.Save(new Note { Title = "Diary", NoteText = "Did [[ run 5 ]] this morning" });

    int changed = storage.UpdateLinksAfterRename("Run 5", "Run 6");

    Assert.Equal(1, changed);
    // The messy link is rewritten neatly, with no stray spaces
    Assert.Contains("[[Run 6]]", storage.Load("Diary")!.NoteText);

    Directory.Delete(folder, true);
}

[Fact]
public void Rename_LeavesOtherLinksAlone()
{
    var (storage, folder) = MakeStorage();
    storage.Save(new Note { Title = "Diary", NoteText = "[[Run 5]] then [[Swim]]" });

    storage.UpdateLinksAfterRename("Run 5", "Run 6");

    string text = storage.Load("Diary")!.NoteText;
    Assert.Contains("[[Swim]]", text);        // untouched
    Assert.Contains("[[Run 6]]", text);       // the one we renamed
    Assert.DoesNotContain("[[Run 5]]", text); // the old link is gone

    Directory.Delete(folder, true);
}

[Fact]
public void Rename_DoesNotMatchLongerTitles()
{
    var (storage, folder) = MakeStorage();
    storage.Save(new Note { Title = "Diary", NoteText = "[[Run 50]] is a different note" });

    int changed = storage.UpdateLinksAfterRename("Run 5", "Run 6");

    Assert.Equal(0, changed);
    Assert.Contains("[[Run 50]]", storage.Load("Diary")!.NoteText);   // "Run 5" must not match inside "Run 50"

    Directory.Delete(folder, true);
}

[Fact]
public void Rename_ReturnsCountOfChangedNotes()
{
    var (storage, folder) = MakeStorage();
    storage.Save(new Note { Title = "One",   NoteText = "[[Run 5]]" });
    storage.Save(new Note { Title = "Two",   NoteText = "also [[Run 5|alias]]" });
    storage.Save(new Note { Title = "Three", NoteText = "no links here" });

    int changed = storage.UpdateLinksAfterRename("Run 5", "Run 6");

    Assert.Equal(2, changed);   // notes One and Two, not Three

    Directory.Delete(folder, true);
}

[Fact]
public void Rename_NoNotesMentionIt_ReturnsZero()
{
    var (storage, folder) = MakeStorage();
    storage.Save(new Note { Title = "Diary", NoteText = "Nothing to see" });

    int changed = storage.UpdateLinksAfterRename("Run 5", "Run 6");

    Assert.Equal(0, changed);

    Directory.Delete(folder, true);
}

[Fact]
public void Rename_DoesNotRewriteUnchangedNotes()
{
    var (storage, folder) = MakeStorage();
    storage.Save(new Note { Title = "Diary", NoteText = "Nothing to see" });

    // Set the file's "last modified" time to a date long ago. If the method
    // rewrites the file, the time will jump to now, and we can see it.
    string file = Directory.GetFiles(folder, "*.md").Single();
    var longAgo = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    File.SetLastWriteTimeUtc(file, longAgo);

    storage.UpdateLinksAfterRename("Run 5", "Run 6");

    Assert.Equal(longAgo, File.GetLastWriteTimeUtc(file));   // still the old time, so it was not saved

    Directory.Delete(folder, true);
}
    
}