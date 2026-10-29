using DiaryApp.Models;

namespace DiaryApp.Tests;

public class FileNamesTests
{
    [Theory] // runs once per [InlineData] row
    [InlineData("Hill route", "Hill-route")]
    [InlineData("Run: hills/flat?", "Run-hillsflat")]
    [InlineData("a  b", "a-b")]
    [InlineData("???", "untitled")]
    public void MakeSafe_Works(string input, string expected) =>
        Assert.Equal(expected, FileNames.MakeSafe(input));
}

// IDisposable: xUnit builds a fresh instance per test, so each test gets its own folder
// and Dispose() deletes it afterwards. Nothing real is ever touched.
public class NoteStorageTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var storage = new NoteStorage(_folder);
        var note = new Note
        {
            Title = "Hill route",
            Exercise = "cycling",
            Mood = Mood.Happy,
            Date = new DateOnly(2026, 10, 28),
            NoteText = "Great ride, see [[Flat route]]."
        };

        storage.Save(note);
        Note? loaded = storage.Load("Hill route");

        Assert.True(File.Exists(Path.Combine(_folder, "Hill-route.md"))); // safe file name
        Assert.NotNull(loaded);
        Assert.Equal("Hill route", loaded!.Title);   // the nice title survived
        Assert.Equal(Mood.Happy, loaded.Mood);
        Assert.Equal(note.Date, loaded.Date);
        Assert.Contains("Flat route", loaded.Links);
    }
}

