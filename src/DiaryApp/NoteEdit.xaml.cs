using DiaryApp.Models;

namespace DiaryApp;

public partial class NoteEdit : ContentPage
{
    private readonly NoteStorage _storage;

    // The app hands us the storage (register this page as transient in MauiProgram.cs)
    public NoteEdit(NoteStorage storage)
    {
        // TODO: keep the storage, then InitializeComponent(), then fill the mood picker
        _storage = storage;
        InitializeComponent();
        MoodPicker.ItemsSource = Enum.GetValues<Mood>();
    }
}