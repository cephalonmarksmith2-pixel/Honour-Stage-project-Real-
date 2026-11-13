using DiaryApp.Models;
using Xunit;

public class GraphLayoutTests
{
    private static Note MakeNote(string title, Mood mood) => new Note { Title = title, Mood = mood };

    [Fact]
    public void ApplyOverview_CentresEachNodeInItsSlot()
    {
        // WHAT: with a width of 900, node i sits in the middle of slot i (150, 450, 750 for three moods).
        // WHY:  this is the left-to-right layout, and Neutral being in the middle depends on it.
        var data = GraphBuilder.BuildOverview(new List<Note>());

        GraphLayout.ApplyOverview(data, 900f, 600f);

        float slotWidth = 900f / data.Nodes.Count;
        for (int i = 0; i < data.Nodes.Count; i++)
        {
            Assert.Equal(slotWidth * (i + 0.5f), data.Nodes[i].X, 3);   // 3 = decimal places allowed
            Assert.Equal(300f, data.Nodes[i].Y, 3);                     // vertically centred
        }
    }

    [Fact]
    public void ApplyOverview_BiggerCountGivesBiggerCircle()
    {
        // WHAT: Happy (3 notes) > Sad (1 note) > Neutral (0 notes) in radius.
        // WHY:  circle size is how the overview shows which moods you write about most.
        var notes = new List<Note>
        {
            MakeNote("A", Mood.Happy), MakeNote("B", Mood.Happy), MakeNote("C", Mood.Happy),
            MakeNote("D", Mood.Sad),
        };
        var data = GraphBuilder.BuildOverview(notes);

        GraphLayout.ApplyOverview(data, 900f, 600f);

        float happy   = data.Nodes.Single(n => n.Mood == Mood.Happy).Radius;
        float sad     = data.Nodes.Single(n => n.Mood == Mood.Sad).Radius;
        float neutral = data.Nodes.Single(n => n.Mood == Mood.Neutral).Radius;
        Assert.True(happy > sad);
        Assert.True(sad > neutral);
    }

    [Theory]
    [InlineData(900f, 600f)]
    [InlineData(300f, 150f)]    // a small screen: circles must still fit
    public void ApplyOverview_EveryCircleFitsOnScreen(float width, float height)
    {
        // WHAT: no circle sticks out past any edge of the screen.
        // WHY:  a circle cut off at the edge looks broken, and a tap there would miss it.
        var notes = Enumerable.Range(0, 20).Select(i => MakeNote("N" + i, Mood.Happy)).ToList();
        var data = GraphBuilder.BuildOverview(notes);

        GraphLayout.ApplyOverview(data, width, height);

        Assert.All(data.Nodes, n =>
        {
            Assert.True(n.X - n.Radius >= 0);
            Assert.True(n.X + n.Radius <= width);
            Assert.True(n.Y - n.Radius >= 0);
            Assert.True(n.Y + n.Radius <= height);
        });
    }

    [Fact]
    public void ApplyOverview_ZeroSize_DoesNotCrashOrMoveAnything()
    {
        // WHAT: a width or height of 0 leaves every node where it was, with no exception.
        // WHY:  the page has no size until it is laid out, and the layout may be called too early.
        var data = GraphBuilder.BuildOverview(new List<Note>());

        GraphLayout.ApplyOverview(data, 0f, 0f);

        Assert.All(data.Nodes, n => Assert.Equal(0f, n.X));
    }
}