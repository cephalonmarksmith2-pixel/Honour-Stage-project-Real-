namespace DiaryApp;
public partial class Graph : ContentPage
{
	// TODO A1: Receive NoteStorage in the constructor (register the page in MauiProgram as transient).
	// TODO A2: OnAppearing: LoadAll, GraphBuilder.Build, then GraphLayout.Apply with the view's
	//          Width and Height. The size is 0 until the page has been laid out. How do you cope with that?
	// TODO A3: Give the drawable the data, then call Invalidate() on the GraphicsView to redraw.
	// TODO A4: Handle taps (GraphicsView.StartInteraction event). Use FindNodeAt, then show the notes.
}

// GraphDrawable.cs
public class GraphDrawable : IDrawable
{
	public GraphData? Data { get; set; }

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		// TODO D1: Draw three zone backgrounds with labels.
		// TODO D2: Draw edges first (so circles sit on top). Line thickness comes from Weight.
		// TODO D3: Draw each node: a circle sized by Radius, coloured by mood, with its exercise name.
		// TODO D4: Nothing to draw? Show a message such as "No notes yet".
	}
	InitializeComponent();

}