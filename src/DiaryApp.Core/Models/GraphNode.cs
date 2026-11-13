namespace DiaryApp.Models;

// What a node stands for on screen.
public enum GraphNodeKind { Mood, Note }

// One circle on the graph. The builder decides WHAT it is; the layout decides WHERE it goes.
public class GraphNode
{
    // ----- Set once by GraphBuilder, read-only afterwards -----

    public string Id { get; init; } = "";            // unique key, e.g. "mood:Happy" or "note:Run 5"
    public string Label { get; init; } = "";         // the text drawn on the circle
    public GraphNodeKind Kind { get; init; }         // mood circle or note circle
    public Mood? Mood { get; init; }                 // which mood it is (mood nodes) or belongs to (note nodes)
    public string? NoteTitle { get; init; }          // note nodes only: lets a tap open the right note
    public string? ParentId { get; init; }           // note nodes only: the node it hangs off (the layout uses this)
    public int Count { get; init; }                  // mood nodes only: how many notes (drives the circle's size)

    // ----- Set later by GraphLayout -----

    public float X { get; set; }                     // centre, in screen units
    public float Y { get; set; }
    public float Radius { get; set; }
}