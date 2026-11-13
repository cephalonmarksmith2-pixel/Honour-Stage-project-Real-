namespace DiaryApp.Models;

// One line between two nodes, found by their ids.
public class GraphEdge
{
    public string FromId { get; init; } = "";
    public string ToId { get; init; } = "";
    public int Weight { get; set; }          // set (not init) because AddEdge makes an existing line heavier
}