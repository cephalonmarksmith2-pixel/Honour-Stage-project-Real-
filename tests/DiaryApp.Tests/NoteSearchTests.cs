using DiaryApp.Models;

namespace DiaryApp.Tests;

public class NoteSearchTests
{
    // ---------- Helpers ----------

    // Helper: build notes that only have titles
    private static List<Note> MakeNotes(params string[] titles) =>
        titles.Select(t => new Note { Title = t }).ToList();

    // Helper: pull the titles out of the results so Assert can compare them in order
    private static string[] Titles(List<Note> notes) =>
        notes.Select(n => n.Title).ToArray();

    // Helper: build a note with a title, mood and date in one line
    private static Note N(string title, Mood mood, int year, int month, int day) =>
        new Note { Title = title, Mood = mood, Date = new DateOnly(year, month, day) };

    // The fixed data set. Work the expected answers out by hand from this table:
    //   Hill route   Happy    2026-10-28
    //   Flat route   Neutral  2026-10-28
    //   Pool swim    Happy    2026-10-27
    //   Sad day      Sad      2026-10-27
    //   Hill sprint  Happy    2026-10-26
    private static List<Note> SampleNotes() => new()
    {
        N("Hill route",  Mood.Happy,   2026, 10, 28),
        N("Flat route",  Mood.Neutral, 2026, 10, 28),
        N("Pool swim",   Mood.Happy,   2026, 10, 27),
        N("Sad day",     Mood.Sad,     2026, 10, 27),
        N("Hill sprint", Mood.Happy,   2026, 10, 26),
    };

    // ---------- Sorting ----------

    [Fact]
    public void EmptyQuery_ReturnsEverythingSorted()
    {
        var notes = MakeNotes("zebra", "Apple", "2 miles", "banana");

        var result = NoteSearch.Find(notes, "");

        // Digits first, then letters, ignoring capitals
        Assert.Equal(new[] { "2 miles", "Apple", "banana", "zebra" }, Titles(result));
    }

    [Fact]
    public void Numbers_SortByValue_NotByCharacter()
    {
        var notes = MakeNotes("Run 10", "Run 2", "Run 1");

        var result = NoteSearch.Find(notes, "");

        // A plain text sort would give: Run 1, Run 10, Run 2
        Assert.Equal(new[] { "Run 1", "Run 2", "Run 10" }, Titles(result));
    }

    // ---------- Mood and date filters ----------

    [Fact]
    public void MoodFilter_ReturnsOnlyThatMood_Sorted()
    {
        var result = NoteSearch.Find(SampleNotes(), "", mood: Mood.Happy);

        // Named argument "mood:" must match the parameter name in Find exactly
        Assert.Equal(new[] { "Hill route", "Hill sprint", "Pool swim" }, Titles(result));
    }

    [Fact]
    public void DateFilter_ReturnsOnlyThatDay_Sorted()
    {
        var result = NoteSearch.Find(SampleNotes(), "", date: new DateOnly(2026, 10, 28));

        Assert.Equal(new[] { "Flat route", "Hill route" }, Titles(result));
    }

    [Fact]
    public void TextAndDate_BothMustMatch()
    {
        var result = NoteSearch.Find(SampleNotes(), "hill", date: new DateOnly(2026, 10, 28));

        // "Hill sprint" matches the text but is dated the 26th, so it is excluded
        Assert.Equal(new[] { "Hill route" }, Titles(result));
    }

    [Fact]
    public void TextAndMoodAndDate_AllMustMatch()
    {
        var result = NoteSearch.Find(SampleNotes(), "hill", mood: Mood.Happy, date: new DateOnly(2026, 10, 28));

        Assert.Equal(new[] { "Hill route" }, Titles(result));
    }

    [Fact]
    public void TextPoolAndMoodSad_MatchesNothing()
    {
        // "Pool swim" matches the text but is Happy; "Sad day" is Sad but isn't called "pool".
        // No single note passes both filters, which proves they combine with AND, not OR.
        var result = NoteSearch.Find(SampleNotes(), "pool", mood: Mood.Sad);

        // The right answer is NO notes (not a list containing an empty title)
        Assert.Empty(result);
    }

    [Fact]
    public void MoodNeutralOnly_ReturnsFlatRoute()
    {
        // query is optional, so we can leave it out and only pass the mood
        var result = NoteSearch.Find(SampleNotes(), mood: Mood.Neutral);

        Assert.Equal(new[] { "Flat route" }, Titles(result));
    }

    [Theory] // runs once for each row
    [InlineData(Mood.Happy, 3)]
    [InlineData(Mood.Sad, 1)]
    [InlineData(Mood.Neutral, 1)]
    public void MoodFilter_CountsMatchTheSampleData(Mood mood, int expectedCount)
    {
        var result = NoteSearch.Find(SampleNotes(), mood: mood);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void DateWithNoNotes_ReturnsEmptyList()
    {
        var result = NoteSearch.Find(SampleNotes(), date: new DateOnly(2026, 1, 1));

        Assert.Empty(result);
    }

    // ---------- Text search ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankQuery_ReturnsAllNotes(string query)
    {
        var notes = MakeNotes("a", "b", "c");

        Assert.Equal(3, NoteSearch.Find(notes, query).Count);
    }

    [Theory]
    [InlineData("route", 2)]   // matches "Hill route" and "Flat route"
    [InlineData("HILL", 1)]    // capitals don't matter
    [InlineData("zzz", 0)]     // no match gives an empty list, not an error
    public void Query_FindsMatchingTitles(string query, int expectedCount)
    {
        var notes = MakeNotes("Hill route", "Flat route", "Pool swim");

        var result = NoteSearch.Find(notes, query);

        Assert.NotNull(result);                    // never null
        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void Query_ResultsAreAlsoSorted()
    {
        var notes = MakeNotes("Route 10", "Route 2", "Pool swim");

        var result = NoteSearch.Find(notes, "route");

        Assert.Equal(new[] { "Route 2", "Route 10" }, Titles(result));
    }
}