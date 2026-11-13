namespace DiaryApp.Models;

// Everything the graph needs to draw: the circles and the lines between them.
public class GraphData
{
    public List<GraphNode> Nodes { get; } = new();
    public List<GraphEdge> Edges { get; } = new();
}