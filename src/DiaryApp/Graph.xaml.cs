namespace DiaryApp;

public partial class Graph : ContentPage
{
    private readonly NoteStorage _storage;
    private readonly GraphDrawable _drawable = new();
    private GraphData? _data;            // what is currently shown
    private Mood? _openMood;             // null = overview; a value = that mood's hub view

    public Graph(NoteStorage storage)
    {
        _storage = storage;
        InitializeComponent();
        GraphView.Drawable = _drawable;  // hand the drawing surface its drawable
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Rebuild();                       // notes may have changed while this tab was away
    }

    // TODO A2: the size is 0 until the layout pass. SizeChanged tells you when it is known.
    private void OnGraphSizeChanged(object? sender, EventArgs e) => Rebuild();

    private void Rebuild()
    {
		var notes = _storage.LoadAll();

    if (_openMood is Mood mood)
    {
        _data = GraphBuilder.BuildMoodView(notes, mood);
        GraphLayout.ApplyMoodView(data, GraphView.Width, GraphView.Height);
    }
    else
    {
        _data = GraphBuilder.BuildOverview(notes);
        GraphLayout.ApplyOverview(data, GraphView.Width, GraphView.Height);
    }

    // TODO 3: give _drawable the data, then redraw (GraphView.Invalidate())
    // TODO 4: show the Back button only when a mood is open
        // TODO 3: _drawable.Data = _data; then GraphView.Invalidate() to redraw.
        // TODO 4: BackButton.IsVisible = _openMood != null
    }

    private async void OnGraphTapped(object? sender, TouchEventArgs e)
    {
        // TODO A4: the tap position is e.Touches[0].X and .Y.
        // TODO 5: GraphLayout.FindNodeAt(_data, x, y). Null? Do nothing.
        // TODO 6: A mood node while in the overview? Set _openMood = node.Mood and Rebuild().
        // TODO 7: A note node? Navigate to the editor:
        //         GoToAsync($"NoteEdit?title={Uri.EscapeDataString(node.NoteTitle!)}&isNew=false")
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        // TODO 8: _openMood = null, then Rebuild().
    }
}