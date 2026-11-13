namespace DiaryApp.Models;

// Decides WHERE each node goes and how big it is. The builder decides WHAT exists.
public static class GraphLayout
{
    // Smallest and largest circle radius, in screen units. The final radius is also capped
    // so the circle always fits inside its slot, whatever the screen size.
    private const float MinRadius = 25f;
    private const float MaxRadius = 80f;
    private const float Margin = 8f;       // breathing room between a circle and its slot edge

// Overview: moods side by side, one per slot
    public static void ApplyOverview(GraphData data, float width, float height)
    {
        // Before the page has a size, or with no nodes, there is nothing sensible to do.
        // This also avoids dividing by zero below.
        if (width <= 0 || height <= 0 || data.Nodes.Count == 0) return;

        // The screen is split into equal slots, one per node (the builder gives them in screen order)
        float slotWidth = width / data.Nodes.Count;

        // Biggest circle that fits: half a slot or half the height, whichever is smaller
        float limit = MathF.Min(slotWidth, height) / 2f - Margin;
        limit = MathF.Max(limit, 1f);                       // on a tiny screen, never zero or negative

        // Both ends of the size range are capped by the limit, so smallest is never above largest
        float smallest = MathF.Min(MinRadius, limit);
        float largest = MathF.Min(MaxRadius, limit);

        int biggestCount = data.Nodes.Max(n => n.Count);    // the busiest mood sets the top of the scale

        for (int i = 0; i < data.Nodes.Count; i++)
        {
            var node = data.Nodes[i];

            node.X = slotWidth * (i + 0.5f);                // the middle of slot i
            node.Y = height / 2f;                           // vertically centred

            // 0 for an empty mood, 1 for the busiest. (float) avoids whole-number division giving 0.
            float share = biggestCount == 0 ? 0f : (float)node.Count / biggestCount;

            node.Radius = smallest + (largest - smallest) * share;
        }
    }
}