using DiaryApp.Models;             // NoteStorage, Note, Mood, NoteWriter, FileNames
using Microsoft.Maui.Dispatching;  // IDispatcherTimer

namespace DiaryApp;

// Shell copies the "title" parameter from the navigation into NoteTitle
[QueryProperty(nameof(NoteTitle), "title")]
[QueryProperty(nameof(IsNew), "isNew")]
public partial class NoteEdit : ContentPage
{
    // ---------- Fields: data the page keeps ----------

    private readonly NoteStorage _storage;   // the way into the notes folder
    private Note? _note;                     // the note being edited (null until it is loaded)
    private string _originalTitle = "";      // the title when editing started (identifies the file and its draft)
    private IDispatcherTimer? _draftTimer;   // counts down after each keystroke, then saves a draft
    private bool _isLoading;                 // true while we fill the boxes, so that doesn't count as "typing"

    // ---------- Properties ----------

    public string IsNew { get; set; } = "";     // "true" when the + button has just created this note
    public string NoteTitle { get; set; } = "";

    // ---------- Constructor ----------

    // The app hands us the storage (this page is registered as transient in MauiProgram.cs)
    public NoteEdit(NoteStorage storage)
    {
        _storage = storage;
        InitializeComponent();
        MoodPicker.ItemsSource = Enum.GetValues<Mood>();   // fills the dropdown with every mood
    }

    // ---------- Page lifecycle ----------

    // async void is allowed here because we may need to ask the user a question (the draft prompt)
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _draftTimer ??= CreateDraftTimer();   // made once, the first time the page appears

        if (_note != null) return;            // already loaded: don't overwrite what the user is editing

        var saved = _storage.Load(NoteTitle);
        if (saved == null)
            return;   // nothing to show (later, this is where a brand-new note will start)

        _originalTitle = saved.Title;
        _note = saved;

        // A leftover draft means the app was closed or killed before the edits were committed
        if (_storage.HasDraft(_originalTitle))
        {
            var draft = _storage.LoadDraft(_originalTitle);

            if (draft == null || NoteWriter.ToFileText(draft) == NoteWriter.ToFileText(saved))
            {
                // Nothing different to recover (for example the user typed and then undid it)
                _storage.DeleteDraft(_originalTitle);
            }
            else
            {
                string when = _storage.DraftSavedAt(_originalTitle)?.ToString("g") ?? "earlier";
                bool keepDraft = await DisplayAlertAsync(
                    "Unsaved changes found",
                    $"A draft from {when} is different from the saved note.\n\n" +
                    $"DRAFT:\n{Preview(draft)}\n\nSAVED:\n{Preview(saved)}",
                    "Keep draft", "Keep saved");

                if (keepDraft)
                    _note = draft;                           // show the draft; leaving the page will commit it
                else
                    _storage.DeleteDraft(_originalTitle);    // throw the draft away
            }
        }

        ShowNoteInBoxes(_note);
    }

    // Leaving the page (back arrow, back button, or navigating elsewhere)
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_note == null) return;   // nothing was loaded, so there is nothing to save or delete

        // "Untouched" means nothing the user deliberately changed:
        // no text, the title is still the generated one, and the exercise is still the starting value
        bool isUntouched =
            string.IsNullOrWhiteSpace(TextEditor.Text)
            && (TitleEntry.Text ?? "").Trim() == _originalTitle
            && (ExerciseEntry.Text ?? "") == _note.Exercise;

        // A brand-new note that was never used: throw it away instead of leaving clutter in the list
        if (IsNew == "true" && isUntouched)
        {
            _draftTimer?.Stop();                  // so a pending countdown can't write a draft afterwards
            _storage.Delete(_originalTitle);
            _storage.DeleteDraft(_originalTitle);
            IsNew = "";
            return;                               // nothing left to commit
        }

        IsNew = "";                               // from now on this page no longer counts as "brand new"
        CopyBoxesIntoNote();
        CommitNote();
    }
    
    // ---------- Event handlers: things the user does ----------

    // Runs on ANY change to the title, exercise, text, mood or date (wired up in NoteEdit.xaml).
    // It doesn't save: it restarts the countdown, so a draft is saved when typing pauses.
    private void OnAnyBoxChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _note == null || _draftTimer == null) return;

        _draftTimer.Stop();    // cancel the countdown in progress...
        _draftTimer.Start();   // ...and start a fresh one
    }

    // Runs when the user taps Save in the top bar (wired up in NoteEdit.xaml)
    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_note == null) return;

        // The Save button is the only trigger that asks: a blank title stops the save
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlertAsync("Title needed", "Please give this note a title.", "OK");
            return;
        }

        CopyBoxesIntoNote();
        CommitNote();

        // Back to the list (it reloads in its own OnAppearing, so it shows the change)
        await Shell.Current.GoToAsync("..");
    }

    // ---------- Helpers ----------

    // Puts a note's values into the boxes. _isLoading stops this counting as typing.
    private void ShowNoteInBoxes(Note note)
    {
        _isLoading = true;

        TitleEntry.Text = note.Title;
        ExerciseEntry.Text = note.Exercise;
        TextEditor.Text = note.NoteText;
        MoodPicker.SelectedItem = note.Mood;
        NoteDatePicker.Date = note.Date.ToDateTime(TimeOnly.MinValue);   // DateOnly -> DateTime

        _isLoading = false;
    }

    // Copies the boxes into _note. Shared by all three triggers; saves nothing.
    private void CopyBoxesIntoNote()
    {
        if (_note == null) return;

        // A blank title quietly falls back to the title we started with
        string typed = (TitleEntry.Text ?? "").Trim();
        _note.Title = typed.Length > 0 ? typed : _originalTitle;

        _note.Exercise = ExerciseEntry.Text ?? "";
        _note.NoteText = TextEditor.Text;
        if (MoodPicker.SelectedItem is Mood mood)
            _note.Mood = mood;
        // The picker's date can be empty, so fall back to today's date
        _note.Date = DateOnly.FromDateTime(NoteDatePicker.Date ?? DateTime.Today);
    }

    // Writes the REAL file, handles a rename, deletes the draft. Used by Save and by leaving.
    private void CommitNote()
    {
        if (_note == null) return;
        _draftTimer?.Stop();

        // TODO 1: Remove spaces from both ends of _note.Title.
        //         (Which string method does that?)

        // TODO 2: Work out whether the FILE NAME changed.
        //         Compare MakeSafe(_originalTitle) with MakeSafe(_note.Title).
        //         Keep the answer in a bool, because you need it twice below.

        // TODO 3: Only if the file name changed AND a file already exists for
        //         the NEW title, swap in a free title.
        //         - Use UniqueTitle here, and only here.
        //         - Update both _note.Title and TitleEntry.Text so the user sees it.
        //         - If the name did NOT change, leave the title alone. The file
        //           existing is normal then, because it is the same note.
        //         (You may need a small "does a file exist for this title?" method
        //         in NoteStorage. Write it in Core so it can be tested.)

        // TODO 4: Save the note.

        // TODO 5: If the name changed, delete the OLD file (_originalTitle).
        //         Once, not twice.

        // TODO 6: Delete the draft that belongs to _originalTitle.

        // TODO 7: Set _originalTitle to exactly what was just saved.
        //         (No UniqueTitle here. The file exists now, so it would add " 2".)
    }

    // Builds the countdown: 1.5 seconds after the LAST change, it saves a draft
    private IDispatcherTimer CreateDraftTimer()
    {
        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1.5);
        timer.IsRepeating = false;            // fires once per pause in typing
        timer.Tick += (s, e) => SaveDraft();
        return timer;
    }

    // A short one-line version of a note's text, for the "keep draft or saved?" pop-up
    private static string Preview(Note note)
    {
        string text = (note.NoteText ?? "").Replace("\n", " ").Trim();
        if (text.Length == 0) return "(no text)";
        return text.Length <= 80 ? text : text[..80] + "...";
    }
}