using DiaryApp.Models;

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

    // The view's size is 0 until the layout pass, so redo the layout when it becomes known
    // (this also fires when the phone is rotated, which is exactly when a re-layout is needed).
    private void OnGraphSizeChanged(object? sender, EventArgs e) => Rebuild();

    // Builds the graph for whatever is open, positions it, and asks for a repaint
    private void Rebuild()
    {
        var notes = _storage.LoadAll();

        // Width and Height are doubles (and -1 before layout); the layout methods return early on <= 0
        float width = (float)GraphView.Width;
        float height = (float)GraphView.Height;

        if (_openMood is Mood mood)
        {
            _data = GraphBuilder.BuildMoodView(notes, mood);
            GraphLayout.ApplyMoodView(_data, width, height);
        }
        else
        {
            _data = GraphBuilder.BuildOverview(notes);
            GraphLayout.ApplyOverview(_data, width, height);
        }

        _drawable.Data = _data;                    // give the drawable the new picture
        GraphView.Invalidate();                    // without this the screen would not repaint
        BackButton.IsVisible = _openMood != null;  // only needed when a mood is open
    }

    // A finger touched the graph
    private async void OnGraphTapped(object? sender, TouchEventArgs e)
    {
        if (_data is null || e.Touches.Length == 0) return;

        PointF touch = e.Touches[0];
        var node = GraphLayout.FindNodeAt(_data, touch.X, touch.Y);
        if (node is null) return;                  // tapped empty space

        if (node.Kind == GraphNodeKind.Mood)
        {
            // In the overview, a mood opens its hub view. Tapping the hub itself does nothing.
            if (_openMood is null)
            {
                _openMood = node.Mood;
                Rebuild();
            }
            return;
        }

        // A note: open it in the editor (isNew=false so it is never deleted as an untouched new note)
        await Shell.Current.GoToAsync(
            $"NoteEdit?title={Uri.EscapeDataString(node.NoteTitle!)}&isNew=false");
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        _openMood = null;
        Rebuild();
    }
}