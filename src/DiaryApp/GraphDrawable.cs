using DiaryApp.Models;
using Microsoft.Maui.Graphics;

namespace DiaryApp;

// Draws the graph. The Graph page owns one of these and gives it fresh data whenever the graph changes.
public class GraphDrawable : IDrawable
{
    public GraphData? Data { get; set; }      // null until the page has something to show

    // The GraphicsView calls this every time it needs to repaint
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Nothing to draw: say so, instead of leaving a blank screen
        if (Data is null || Data.Nodes.Count == 0)
        {
            canvas.FontColor = Colors.Gray;
            canvas.FontSize = 18;
            canvas.DrawString("No notes yet",
                dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        // Edges FIRST, so the circles sit on top of the lines
        var byId = Data.Nodes.ToDictionary(n => n.Id);     // find a node from its id
        canvas.StrokeColor = Colors.Gray;

        foreach (var edge in Data.Edges)
        {
            // Skip a line if either end is missing, instead of crashing
            if (!byId.TryGetValue(edge.FromId, out var from)) continue;
            if (!byId.TryGetValue(edge.ToId, out var to)) continue;

            canvas.StrokeSize = 2 + edge.Weight;           // a heavier connection is a thicker line
            canvas.DrawLine(from.X, from.Y, to.X, to.Y);
        }

        // Then each node: a filled circle with its label
        foreach (var node in Data.Nodes)
        {
            canvas.FillColor = MoodColour(node.Mood);
            canvas.FillCircle(node.X, node.Y, node.Radius);

            // Mood nodes also show how many notes they hold
            string text = node.Kind == GraphNodeKind.Mood
                ? $"{node.Label} ({node.Count})"
                : node.Label;

            canvas.FontColor = Colors.White;
            canvas.FontSize = Math.Clamp(node.Radius * 0.35f, 9f, 18f);   // text scales with the circle
            canvas.DrawString(text,
                node.X - node.Radius, node.Y - node.Radius,   // a box exactly covering the circle
                node.Radius * 2, node.Radius * 2,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }

    // One colour per mood. A mood added to the enum later falls into the default until it is given its own.
    private static Color MoodColour(Mood? mood) => mood switch
    {
        Mood.Happy   => Colors.DarkOrange,
        Mood.Neutral => Colors.SlateGray,
        Mood.Sad     => Colors.SteelBlue,
        _            => Colors.MediumPurple
    };
}