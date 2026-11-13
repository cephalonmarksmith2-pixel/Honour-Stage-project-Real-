namespace DiaryApp.Models;

// Turns notes into graph data: WHICH nodes and lines exist. It never decides positions or sizes.
public static class GraphBuilder
{
    // The prefixes stop a note called "Happy" clashing with the Happy mood node's id.
    private const string MoodPrefix = "mood:";
    private const string NotePrefix = "note:";

    // ---------- Overview: one node per mood ----------

    public static GraphData BuildOverview(List<Note> notes)
    {
        var data = new GraphData();

        // One pass per mood (not per note), so a mood with zero notes still gets a node.
        // Enum order is declaration order, which is the left-to-right order on screen.
        foreach (Mood mood in Enum.GetValues<Mood>())
        {
            data.Nodes.Add(new GraphNode
            {
                Id = MoodId(mood),
                Label = mood.ToString(),
                Kind = GraphNodeKind.Mood,
                Mood = mood,
                Count = notes.Count(n => n.Mood == mood)
            });
        }

        // No edges here: a note has only one mood, so nothing joins one mood to another.
        return data;
    }

    // ---------- Mood view: the mood as a hub, its notes around it, linked notes beyond ----------

    public static GraphData BuildMoodView(List<Note> notes, Mood mood)
    {
        var data = new GraphData();
        var byId = new Dictionary<string, GraphNode>();   // one entry per id, so duplicates are impossible

        // Only the notes with this mood
        var moodNotes = notes.Where(n => n.Mood == mood).ToList();

        // 1. The hub
        string hubId = MoodId(mood);
        byId[hubId] = new GraphNode
        {
            Id = hubId,
            Label = mood.ToString(),
            Kind = GraphNodeKind.Mood,
            Mood = mood,
            Count = moodNotes.Count
        };

        // 2. One node per note of this mood, each joined to the hub
        foreach (var note in moodNotes)
        {
            byId[NoteId(note)] = MakeNoteNode(note, parentId: hubId);
            AddEdge(data, hubId, NoteId(note));
        }

        // 3. Follow the links, one level deep: only THIS mood's notes are searched for links
        foreach (var source in moodNotes)
        {
            string sourceId = NoteId(source);

            foreach (string raw in NoteLinks.FindLinks(source.NoteText))
            {
                Note? target = NoteLinks.Resolve(NoteLinks.ParseLink(raw).Title, notes);
                if (target is null) continue;                // missing note: skip, never create one here

                string targetId = NoteId(target);

                // Make a node only if there isn't one already (it may be another note of this
                // mood, or already linked from somewhere else). This keeps each note to one node.
                if (!byId.ContainsKey(targetId))
                    byId[targetId] = MakeNoteNode(target, parentId: sourceId);

                AddEdge(data, sourceId, targetId);
            }
        }

        // 4. Hand over the nodes
        data.Nodes.AddRange(byId.Values);
        return data;
    }

    // ---------- Helpers (private: only the builder needs them) ----------

    private static string MoodId(Mood mood) => MoodPrefix + mood;
    private static string NoteId(Note note) => NotePrefix + note.Title;

    // Builds a note node. parentId is the node it hangs off (the hub, or the note that linked to it).
    private static GraphNode MakeNoteNode(Note note, string parentId) => new GraphNode
    {
        Id = NoteId(note),
        Label = note.Title,
        Kind = GraphNodeKind.Note,
        Mood = note.Mood,
        NoteTitle = note.Title,
        ParentId = parentId
    };

    // Adds a line, or makes an existing one heavier. A line from a node to itself is ignored.
    // A to B and B to A count as different lines (kept simple for now).
    private static void AddEdge(GraphData data, string from, string to)
    {
        if (from == to) return;

        var existing = data.Edges.FirstOrDefault(e => e.FromId == from && e.ToId == to);
        if (existing != null)
            existing.Weight++;
        else
            data.Edges.Add(new GraphEdge { FromId = from, ToId = to, Weight = 1 });
    }
}