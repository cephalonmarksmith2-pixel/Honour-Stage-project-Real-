using DiaryApp.Models;

namespace DiaryApp.Tests;

// IDisposable: xUnit builds a fresh instance of this class for every test, so each test gets its own
// temporary folder, and Dispose() deletes it afterwards. Nothing real is ever touched.
public class NoteStorageDraftTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    [Fact]
    public void NoDraft_HasDraftIsFalse_AndLoadDraftIsNull()
    {
        var storage = new NoteStorage(_folder);

        Assert.False(storage.HasDraft("Hill route"));
        Assert.Null(storage.LoadDraft("Hill route"));
    }

    [Fact]
    public void SaveDraft_DoesNotTouchTheRealNote_AndIsNotListed()
    {
        var storage = new NoteStorage(_folder);
        storage.Save(new Note { Title = "Hill route", NoteText = "original" });

        storage.SaveDraft("Hill route", new Note { Title = "Hill route", NoteText = "edited" });

        Assert.Equal("original", storage.Load("Hill route")!.NoteText);   // real note unchanged
        Assert.Single(storage.LoadAll());                                  // the draft is not a second note
        Assert.True(storage.HasDraft("Hill route"));
    }

    [Fact]
    public void LoadDraft_ReturnsTheEditedContent_EvenIfTheTitleChanged()
    {
        var storage = new NoteStorage(_folder);

        // Filed under the ORIGINAL title, but the user has typed a new one
        storage.SaveDraft("Hill route", new Note { Title = "Hill route 2", NoteText = "edited" });
        Note? draft = storage.LoadDraft("Hill route");

        Assert.NotNull(draft);
        Assert.Equal("Hill route 2", draft!.Title);
        Assert.Equal("edited", draft.NoteText);
    }

    [Fact]
    public void DeleteDraft_RemovesIt_AndIsSafeToCallTwice()
    {
        var storage = new NoteStorage(_folder);
        storage.SaveDraft("Hill route", new Note { Title = "Hill route", NoteText = "edited" });

        storage.DeleteDraft("Hill route");
        storage.DeleteDraft("Hill route");   // no draft left: must not throw

        Assert.False(storage.HasDraft("Hill route"));
    }

    [Fact]
    public void Save_OverwritesTheNote_AndLeavesNoTempFileBehind()
    {
        var storage = new NoteStorage(_folder);
        storage.Save(new Note { Title = "Hill route", NoteText = "first" });

        storage.Save(new Note { Title = "Hill route", NoteText = "second" });

        Assert.Equal("second", storage.Load("Hill route")!.NoteText);
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));   // the safe-write temp file was swapped away
    }
}