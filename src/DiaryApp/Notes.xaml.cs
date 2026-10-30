using DiaryApp.Models;   // so NoteStorage can be found

namespace DiaryApp;

public partial class Notes : ContentPage
{
    // Kept in a field so the page's other methods can use it later
    private readonly NoteStorage _storage;

    // The app hands us the storage when it builds the page (registered in MauiProgram.cs)
    public Notes(NoteStorage storage)
    {
        _storage = storage;       // 1. keep it
        InitializeComponent();    // 2. build the controls (StatusLabel only exists after this line)

        // 3. load every note from the folder and show how many there are
        var notes = _storage.LoadAll();
        StatusLabel.Text = $"{notes.Count} notes";
    }
}