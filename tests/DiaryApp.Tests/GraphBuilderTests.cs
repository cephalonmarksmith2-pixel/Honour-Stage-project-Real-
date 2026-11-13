using DiaryApp.Models;
using Xunit;

public class GraphBuilderTests
{
    // Builds a note in one line, so each test stays short and the data is easy to read.
    private static Note MakeNote(string title, Mood mood, string text = "")
        => new Note { Title = title, Mood = mood, NoteText = text };

    // =====================================================================
    // BuildOverview: one node per mood
    // =====================================================================

    [Fact]
    public void BuildOverview_EmptyList_StillHasANodePerMood()
    {
        // WHAT: with no notes at all, every mood still gets a node, each with Count 0.
        // WHY:  the screen layout is based on the moods, not on the notes. If an empty
        //       mood vanished, the others would shift position as notes came and went.

        var data = GraphBuilder.BuildOverview(new List<Note>());

        // One node per value of the enum, however many moods exist now or later
        Assert.Equal(Enum.GetValues<Mood>().Length, data.Nodes.Count);

        // Assert.All runs the check on every node
        Assert.All(data.Nodes, node => Assert.Equal(0, node.Count));
    }

    [Fact]
    public void BuildOverview_CountsNotesPerMood()
    {
        // WHAT: two Happy notes, one Sad, none Neutral give counts of 2, 1 and 0.
        // WHY:  Count drives the size of each circle, so a wrong count is a wrong picture.

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy),
            MakeNote("Run 6", Mood.Happy),
            MakeNote("Swim",  Mood.Sad),
        };

        var data = GraphBuilder.BuildOverview(notes);

        Assert.Equal(2, data.Nodes.Single(n => n.Mood == Mood.Happy).Count);
        Assert.Equal(1, data.Nodes.Single(n => n.Mood == Mood.Sad).Count);
        Assert.Equal(0, data.Nodes.Single(n => n.Mood == Mood.Neutral).Count);   // empty mood, still there
    }

    [Fact]
    public void BuildOverview_NodesAreInEnumOrder()
    {
        // WHAT: the nodes come out in the same order as the Mood enum (Happy, Neutral, Sad).
        // WHY:  the layout places moods left to right in this order. If it changed,
        //       Neutral could stop being in the middle.

        var data = GraphBuilder.BuildOverview(new List<Note>());

        // Mood is nullable on a node, so .Value turns Mood? into Mood for the comparison
        var moodsInNodeOrder = data.Nodes.Select(n => n.Mood!.Value).ToList();

        Assert.Equal(Enum.GetValues<Mood>(), moodsInNodeOrder);
    }

    [Fact]
    public void BuildOverview_HasNoEdges_EvenWhenNotesLinkAcrossMoods()
    {
        // WHAT: notes that link to each other do not create lines in the overview.
        // WHY:  I decided cross-mood lines belong to the mood view. This test records
        //       that decision, so a later change has to be deliberate.

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy, "then a swim: [[Swim]]"),
            MakeNote("Swim",  Mood.Sad,   "cold"),
        };

        var data = GraphBuilder.BuildOverview(notes);

        Assert.Empty(data.Edges);
    }

    [Fact]
    public void BuildOverview_NodeIdsAreUniqueAndPrefixed()
    {
        // WHAT: every id is different, and starts with "mood:".
        // WHY:  edges and the layout find nodes by id. A repeated id would silently
        //       merge two circles into one.

        var data = GraphBuilder.BuildOverview(new List<Note>());

        Assert.All(data.Nodes, node => Assert.StartsWith("mood:", node.Id));
        Assert.Equal(data.Nodes.Count, data.Nodes.Select(n => n.Id).Distinct().Count());
    }

    // =====================================================================
    // BuildMoodView: a mood as the hub, its notes around it
    // =====================================================================

    [Fact]
    public void BuildMoodView_HasHubAndOnlyNotesOfThatMood()
    {
        // WHAT: Happy view of two Happy notes and one Sad note gives a hub plus the two Happy notes.
        // WHY:  the Sad note must not appear in the Happy view.

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy),
            MakeNote("Run 6", Mood.Happy),
            MakeNote("Swim",  Mood.Sad),
        };

        var data = GraphBuilder.BuildMoodView(notes, Mood.Happy);

        Assert.Equal(3, data.Nodes.Count);                              // hub + 2 Happy notes
        Assert.Contains(data.Nodes, n => n.Id == "mood:Happy");         // the hub is there
        Assert.DoesNotContain(data.Nodes, n => n.Id == "note:Swim");    // the Sad note is not
    }

    [Fact]
    public void BuildMoodView_EveryNoteIsJoinedToTheHub()
    {
        // WHAT: each note of the mood has an edge from the hub ("mood:Happy") to it.
        // WHY:  the hub-and-spoke picture depends on these lines, and the layout uses
        //       each note's ParentId to place it around the hub.

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy),
            MakeNote("Run 6", Mood.Happy),
        };

        var data = GraphBuilder.BuildMoodView(notes, Mood.Happy);

        foreach (string id in new[] { "note:Run 5", "note:Run 6" })
        {
            Assert.Contains(data.Edges, e => e.FromId == "mood:Happy" && e.ToId == id);   // the line exists
            Assert.Equal("mood:Happy", data.Nodes.Single(n => n.Id == id).ParentId);      // and the parent is the hub
        }
    }

    [Fact]
    public void BuildMoodView_MoodWithNoNotes_HasOnlyTheHub()
    {
        // WHAT: viewing a mood nobody used gives one node (the hub) and no edges.
        // WHY:  an empty list must not crash or invent nodes. It's the first real-world case
        //       (a new user taps Sad before writing any sad notes).

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy),
        };

        var data = GraphBuilder.BuildMoodView(notes, Mood.Sad);

        var onlyNode = Assert.Single(data.Nodes);       // fails if there are 0 or 2+ nodes
        Assert.Equal("mood:Sad", onlyNode.Id);
        Assert.Empty(data.Edges);
    }

    [Fact]
    public void BuildMoodView_LinkToOtherMood_AddsOuterNodeHangingOffSource()
    {
        // WHAT: a Happy note links to a Sad note. In the Happy view, the Sad note appears
        //       as an outer node, attached to the note that linked to it.
        // WHY:  linked notes are what makes this a graph, not just a list. The ParentId
        //       matters because the layout uses it to place the outer node near its source.

        // ARRANGE: notes with real titles, moods and a link in the text
        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy, "Felt great, then swam: [[Swim]]"),
            MakeNote("Swim",  Mood.Sad,   "Cold day"),
        };

        // ACT
        var data = GraphBuilder.BuildMoodView(notes, Mood.Happy);

        // ASSERT
        // Three nodes: the hub, "Run 5" (a Happy note), and "Swim" (outer, because it is Sad)
        Assert.Equal(3, data.Nodes.Count);

        // Find the outer node by its id and check who it hangs off
        var swim = data.Nodes.Single(n => n.Id == "note:Swim");
        Assert.Equal("note:Run 5", swim.ParentId);

        // A line from the note that linked to the note it linked to
        Assert.Contains(data.Edges, e => e.FromId == "note:Run 5" && e.ToId == "note:Swim");
    }

    [Fact]
    public void BuildMoodView_LinkToMissingNote_AddsNothing()
    {
        // WHAT: a link to a note that doesn't exist adds no node and no edge.
        // WHY:  building a graph must never invent notes or draw lines into nothing.

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy, "see [[Nothing here]]"),    // no note called "Nothing here" exists
        };

        var data = GraphBuilder.BuildMoodView(notes, Mood.Happy);

        Assert.Equal(2, data.Nodes.Count);                       // just the hub and "Run 5"
        var onlyEdge = Assert.Single(data.Edges);                // just hub -> Run 5
        Assert.Equal("mood:Happy", onlyEdge.FromId);
        Assert.Equal("note:Run 5", onlyEdge.ToId);
    }

    [Fact]
    public void BuildMoodView_NoteLinkedFromTwoPlaces_IsOneNode()
    {
        // WHAT: two Happy notes both link to the same Sad note. It appears once, with two lines into it.
        // WHY:  the "must be one node, not two" rule. A duplicate circle would overlap the first
        //       and look like a bug.

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy, "See sad Swim [[Swim]]"),
            MakeNote("Run 6", Mood.Happy, "See sad Swim [[Swim]]"),
            MakeNote("Swim",  Mood.Sad,   "dees"),
        };

        var data = GraphBuilder.BuildMoodView(notes, Mood.Happy);

        Assert.Single(data.Nodes, n => n.Id == "note:Swim");                    // exactly one Swim node
        Assert.Equal(4, data.Nodes.Count);                                      // hub, Run 5, Run 6, Swim
        Assert.Equal(2, data.Edges.Count(e => e.ToId == "note:Swim"));          // one line in from each Run note
    }

    [Fact]
    public void BuildMoodView_LinkToSameMoodNote_AddsLineButNoExtraNode()
    {
        // WHAT: a Happy note linking to another Happy note adds a line between them, but no new node.
        // WHY:  that note already has a node from the ring, so the dictionary check must reuse it.

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy, "next time, [[Run 6]]"),
            MakeNote("Run 6", Mood.Happy, ""),
        };

        var data = GraphBuilder.BuildMoodView(notes, Mood.Happy);

        Assert.Equal(3, data.Nodes.Count);                                                    // hub + 2 notes, nothing extra
        Assert.Contains(data.Edges, e => e.FromId == "note:Run 5" && e.ToId == "note:Run 6"); // the link line
        // The ring node keeps the hub as its parent: the link must not overwrite it
        Assert.Equal("mood:Happy", data.Nodes.Single(n => n.Id == "note:Run 6").ParentId);
    }

    [Fact]
    public void BuildMoodView_LinkWithCapitalsAndAlias_StillResolves()
    {
        // WHAT: "[[ run 5 | my 5k ]]" finds the note "Run 5".
        // WHY:  links are typed by people. The graph must follow the same matching
        //       rules the editor uses (ignore capitals and spaces, ignore the alias).

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Sad,   ""),
            MakeNote("Diary", Mood.Happy, "see [[ run 5 | my 5k ]]"),
        };

        var data = GraphBuilder.BuildMoodView(notes, Mood.Happy);

        // The edge points at the note's REAL title ("Run 5"), not at what was typed
        Assert.Contains(data.Edges, e => e.FromId == "note:Diary" && e.ToId == "note:Run 5");
        Assert.Single(data.Nodes, n => n.Id == "note:Run 5");
    }

    [Fact]
    public void BuildMoodView_NoteLinkingToItself_AddsNoLine()
    {
        // WHAT: a note whose text links to its own title gets no self-loop.
        // WHY:  a line from a circle to itself is just noise.

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy, "this is [[Run 5]] again"),
        };

        var data = GraphBuilder.BuildMoodView(notes, Mood.Happy);

        Assert.DoesNotContain(data.Edges, e => e.FromId == e.ToId);
        Assert.Equal(2, data.Nodes.Count);       // hub + the note, no duplicate
    }

    [Fact]
    public void BuildMoodView_DoesNotFollowLinksOfOuterNotes()
    {
        // WHAT: Happy note links to Sad note, and the Sad note links to a Neutral note.
        //       The Neutral note does not appear in the Happy view.
        // WHY:  I decided to go one level deep. Following links all the way could pull in the whole diary.
        //       This test records the limit.

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Happy,   "then [[Swim]]"),
            MakeNote("Swim",  Mood.Sad,     "then [[Hill]]"),
            MakeNote("Hill",  Mood.Neutral, ""),
        };

        var data = GraphBuilder.BuildMoodView(notes, Mood.Happy);

        Assert.Equal(3, data.Nodes.Count);                                  // hub, Run 5, Swim
        Assert.DoesNotContain(data.Nodes, n => n.Id == "note:Hill");        // two links away: not shown
    }

    [Fact]
    public void BuildMoodView_TwoDifferentLinksToSameNote_MakesTheLineHeavier()
    {
        // WHAT: "[[Run 5]]" and "[[run 5|my 5k]]" in one note both resolve to the same target.
        //       There is one edge, with Weight 2.
        // WHY:  Weight is how the drawing code will make busy connections thicker, and this
        //       checks the "heavier, not duplicated" behaviour of AddEdge.
        //       (The two links are written differently on purpose: identical ones would be
        //       merged earlier, when the links are listed.)

        var notes = new List<Note>
        {
            MakeNote("Run 5", Mood.Sad,   ""),
            MakeNote("Diary", Mood.Happy, "[[Run 5]] and later [[run 5|my 5k]]"),
        };

        var data = GraphBuilder.BuildMoodView(notes, Mood.Happy);

        // Assert.Single fails if there are none, or if the line was added twice
        var edge = Assert.Single(data.Edges, e => e.FromId == "note:Diary" && e.ToId == "note:Run 5");
        Assert.Equal(2, edge.Weight);
    }
}

public class ExerciseKeyTests
{
    [Theory]
    [InlineData("run", "run")]
    [InlineData("RUN", "run")]
    [InlineData("  Run ", "run")]
    [InlineData("Hill Sprint", "hill sprint")]
    public void From_IgnoresCapitalsAndEdgeSpaces(string input, string expected)
    {
        // WHAT: different spellings of the same exercise produce one key.
        // WHY:  "Run", "RUN" and " run " must group together.

        Assert.Equal(expected, ExerciseKey.From(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_BlankOrMissing_IsUnassigned(string? input)
    {
        // WHAT: null, empty and spaces-only all become "unassigned".
        // WHY:  a note with no exercise must not crash, and must group with the other blank ones.

        Assert.Equal("unassigned", ExerciseKey.From(input));
    }
}