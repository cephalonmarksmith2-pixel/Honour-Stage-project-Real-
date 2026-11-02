using DiaryApp.Models;   // so NoteStorage, Note, Mood and NoteSearch can be found

namespace DiaryApp;

public partial class Notes : ContentPage
{
    // ---------- Fields: data the page keeps ----------

    private readonly NoteStorage _storage;     // the way into the notes folder
    private List<Note> _allNotes = new();      // every note, loaded once; searching filters this list

    // ---------- Constructor ----------

    // The app hands us the storage when it builds the page
    public Notes(NoteStorage storage)
    {
        _storage = storage;
        InitializeComponent();
    }

    // ---------- Page lifecycle ----------

    // Runs every time the page is shown (tab pages aren't rebuilt, so the loading goes here)
    protected override void OnAppearing()
    {
        base.OnAppearing();   // always first
#if DEBUG
        // Test data only: if the folder is empty, save the sample notes
        if (_storage.LoadAll().Count == 0)
        {
            foreach (var note in SampleNotes())
            {
                _storage.Save(note);
            }
        }
#endif

        // The Singel read from disk, after any seeding, so it sees the saved notes
        _allNotes = _storage.LoadAll();
        ShowNotes("");   // an empty search shows everything, sorted
    }

    // ---------- Event handlers: things the user does ----------
// Runs when the user taps the add button (wired up in Notes.xaml)
private async void OnAddClicked(object? sender, EventArgs e)
{
    // Makes and SAVES a new note ("Untitled", or "Untitled 2" if that's taken) and hands it back
    var note = _storage.CreateNew();

    // Open it in the editor, plus a marker saying it's brand new
    await Shell.Current.GoToAsync($"NoteEdit?title={Uri.EscapeDataString(note.Title)}&isNew=true");
}

    // Runs every time the text in the search box changes
    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // e.NewTextValue is what's in the box now; ?? "" turns null into an empty search
        ShowNotes(e.NewTextValue ?? "");
    }

    // Runs when the user taps a note in the list
    private async void OnNoteSelected(object? sender, SelectionChangedEventArgs e)
    {
    // 1. Read the first selected item and treat it as a Note.
    //    "as Note" gives null if it isn't a Note, and FirstOrDefault gives null if the list is empty.
    var note = e.CurrentSelection.FirstOrDefault() as Note;

    // 2. Nothing selected? Do nothing. 
    if (note == null)
        return;

    // 3. Open the editor, passing the title in a parameter called "title".
    //    EscapeDataString makes spaces and symbols safe to put in the text (i.e. "Long run","27 mile run!!").
    await Shell.Current.GoToAsync($"NoteEdit?title={Uri.EscapeDataString(note.Title)}");

    // 4. Clear the selection, so tapping the same note again works
    NotesList.SelectedItem = null;
    }

    // ---------- Helpers ----------

    // Runs the search over the notes we already hold, and updates the list and the count
    private void ShowNotes(string query)
    {
        var results = NoteSearch.Find(_allNotes, query);
        NotesList.ItemsSource = results;                // the list shows these notes
        StatusLabel.Text = $"{results.Count} notes";    // the count matches what's shown
    }

    // ---------- Sample data (used only in debug builds) ----------

    private static Note N(string title, Mood mood, int year, int month, int day) =>
        new Note { Title = title, Mood = mood, Date = new DateOnly(year, month, day) };

    private static List<Note> SampleNotes() => new()
    {
        N("Hill route",  Mood.Happy,   2026, 10, 28),
        N("Flat route",  Mood.Neutral, 2026, 10, 28),
        N("Pool swim",   Mood.Happy,   2026, 10, 27),
        N("Sad day",     Mood.Sad,     2026, 10, 27),
        N("Hill sprint", Mood.Happy,   2026, 10, 26),
    };
}