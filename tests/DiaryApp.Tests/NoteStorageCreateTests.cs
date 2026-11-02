using DiaryApp.Models;

namespace DiaryApp.Tests;

// Each test gets its own temporary folder, deleted afterwards (same pattern as the other storage tests)
public class NoteStorageCreateTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    [Fact]
    public void UniqueTitle_ReturnsTheBaseTitle_WhenItIsFree()
    {
        var storage = new NoteStorage(_folder);

        Assert.Equal("Untitled", storage.UniqueTitle("Untitled"));
    }

    [Fact]
    public void UniqueTitle_AddsTwo_WhenTheTitleExists()
    {
        var storage = new NoteStorage(_folder);
        storage.Save(new Note { Title = "Untitled" });

        Assert.Equal("Untitled 2", storage.UniqueTitle("Untitled"));
    }

    [Fact]
    public void UniqueTitle_AddsThree_WhenTwoExist()
    {
        var storage = new NoteStorage(_folder);
        storage.Save(new Note { Title = "Untitled" });
        storage.Save(new Note { Title = "Untitled 2" });

        Assert.Equal("Untitled 3", storage.UniqueTitle("Untitled"));
    }

    [Fact]
    public void UniqueTitle_TreatsTitlesWithTheSameFileNameAsTaken()
    {
        var storage = new NoteStorage(_folder);
        storage.Save(new Note { Title = "my note" });    // file: my-note.md

        // "my-note" would be saved to the same file, so it counts as taken
        Assert.Equal("my-note 2", storage.UniqueTitle("my-note"));
    }

    [Fact]
    public void UniqueTitle_BlankBaseTitle_FallsBackToUntitled()
    {
        var storage = new NoteStorage(_folder);

        Assert.Equal("Untitled", storage.UniqueTitle("   "));
    }

    [Fact]
    public void CreateNew_SavesAnEmptyNoteWithDefaults()
    {
        var storage = new NoteStorage(_folder);

        var created = storage.CreateNew("Run 21 Oct 2026", "run");
        var loaded = storage.Load(created.Title);

        Assert.NotNull(loaded);
        Assert.Equal("Run 21 Oct 2026", loaded!.Title);
        Assert.Equal("run", loaded.Exercise);
        Assert.Equal(Mood.Neutral, loaded.Mood);
        Assert.Equal("", loaded.NoteText);
    }

    [Fact]
    public void CreateNew_CalledTwice_MakesTwoDifferentNotes()
    {
        var storage = new NoteStorage(_folder);

        var first = storage.CreateNew();
        var second = storage.CreateNew();

        Assert.Equal("Untitled", first.Title);
        Assert.Equal("Untitled 2", second.Title);
        Assert.Equal(2, storage.LoadAll().Count);
    }
}