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
    // Returns the node under a tap, or null if the tap hit empty space.
// If circles overlap, the node whose centre is nearest the tap wins.
public static GraphNode? FindNodeAt(GraphData data, float x, float y)
{
    GraphNode? best = null;
    float bestDistance = float.MaxValue;

    foreach (var node in data.Nodes)
    {
        float dx = x - node.X;
        float dy = y - node.Y;
        float distanceSquared = dx * dx + dy * dy;      // comparing squares avoids a square root

        if (distanceSquared <= node.Radius * node.Radius && distanceSquared < bestDistance)
        {
            best = node;
            bestDistance = distanceSquared;
        }
    }

    return best;
}

// Mood view: the hub in the centre, its notes on a ring, linked notes further out
public static void ApplyMoodView(GraphData data, float width, float height)
{
    if (width <= 0 || height <= 0 || data.Nodes.Count == 0) return;

    var hub = data.Nodes.FirstOrDefault(n => n.Kind == GraphNodeKind.Mood);
    if (hub is null) return;

    float centreX = width / 2f;
    float centreY = height / 2f;
    float half = MathF.Min(width, height) / 2f;          // distances are fractions of this, so it always fits

    // Ring notes hang off the hub; everything else is an outer note hanging off a ring note
    var ring = data.Nodes.Where(n => n.ParentId == hub.Id).ToList();
    var outer = data.Nodes.Where(n => n != hub && n.ParentId != hub.Id).ToList();

    hub.X = centreX;
    hub.Y = centreY;
    hub.Radius = MathF.Max(half * 0.18f, 20f);

    // Remember each ring note's angle, so its linked notes can use it
    var angleOf = new Dictionary<string, float>();
    float step = ring.Count > 0 ? 2f * MathF.PI / ring.Count : 2f * MathF.PI;   // the angle between neighbours

    for (int i = 0; i < ring.Count; i++)
    {
        float angle = step * i - MathF.PI / 2f;          // start at the top, then go round like a clock
        ring[i].X = centreX + half * 0.5f * MathF.Cos(angle);
        ring[i].Y = centreY + half * 0.5f * MathF.Sin(angle);
        ring[i].Radius = MathF.Max(half * 0.09f, 14f);
        angleOf[ring[i].Id] = angle;
    }

    // Outer notes: the same direction as their parent, further out, fanned out if there are several
    foreach (var group in outer.GroupBy(n => n.ParentId))
    {
        var children = group.ToList();
        float parentAngle = angleOf.TryGetValue(group.Key ?? "", out var a) ? a : 0f;
        float gap = step / (children.Count + 1);         // keeps the fan inside the parent's slice

        for (int k = 0; k < children.Count; k++)
        {
            float angle = parentAngle + (k - (children.Count - 1) / 2f) * gap;
            children[k].X = centreX + half * 0.85f * MathF.Cos(angle);
            children[k].Y = centreY + half * 0.85f * MathF.Sin(angle);
            children[k].Radius = MathF.Max(half * 0.07f, 10f);
        }
    }
 }
}