using DiaryApp.Models;

namespace DiaryApp.Tests;

public class NoteParserTests
{
    [Fact] // [Fact] marks a method as a test
    public void Parse_ReadsTagsAndLinks()
    {
        var note = NoteParser.Parse("Run",
            "[exercise:run][date:2026-10-28][mood:happy] Great run today, see [[Hill route]].");

        // Assert.Equal(expected, actual): the test fails if they differ
        Assert.Equal(Mood.Happy, note.Mood);
        Assert.Equal("run", note.Exercise);
        Assert.Equal(new DateOnly(2026, 10, 28), note.Date);
        Assert.Contains("Hill route", note.Links);
        Assert.Equal("Great run today, see [[Hill route]].", note.NoteText);
    }
}