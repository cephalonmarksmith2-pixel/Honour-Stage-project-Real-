namespace DiaryApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Run once at startup: makes the route name "NoteEdit" mean "open the NoteEdit page"
		Routing.RegisterRoute("NoteEdit", typeof(NoteEdit));
	}
}
