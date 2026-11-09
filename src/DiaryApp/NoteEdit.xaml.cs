using DiaryApp.Models;             // NoteStorage, Note, Mood, NoteWriter, FileNames, NoteSearch
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
            return;   // nothing to show

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

    // ---------- Event handlers: editing the note ----------

    // Runs on ANY change to the title, exercise, text, mood or date (wired up in NoteEdit.xaml).
    // It doesn't save: it restarts the countdown, so a draft is saved when typing pauses.
    private void OnAnyBoxChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _note == null || _draftTimer == null) return;

        RefreshLinks();        // the text may have gained or lost a [[link]]

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

    // ---------- Event handlers: links ----------

    // A link in the "links in this note" list was tapped: open that note, creating it if missing
    private async void OnLinkTapped(object? sender, SelectionChangedEventArgs e)
    {
        if (_note == null) return;

        // The selected item is the raw link text, e.g. "[[Run 5|my 5k]]". "is not string" also covers null.
        if (e.CurrentSelection.FirstOrDefault() is not string tapped) return;

        LinksList.SelectedItem = null;   // clear it now, so the same link can be tapped again later

        string title = NoteLinks.ParseLink(tapped).Title;   // strips [[ ]], trims, drops any |alias
        if (string.IsNullOrWhiteSpace(title)) return;       // "[[]]" has nothing to open

        // Save what the user has typed BEFORE leaving, so no edits are lost
        CopyBoxesIntoNote();
        CommitNote();

        var (target, created) = _storage.OpenOrCreate(title);

        if (created)
            await DisplayAlertAsync("Note created", $"\"{target.Title}\" was created.", "OK");

        // isNew=false: the user has just been told this note exists, so it should stay even if left blank
        await Shell.Current.GoToAsync($"NoteEdit?title={Uri.EscapeDataString(target.Title)}&isNew=false");
    }

    // "Add link" button: show or hide the search panel
    private void OnAddLinkClicked(object? sender, EventArgs e)
    {
        LinkPanel.IsVisible = !LinkPanel.IsVisible;

        if (LinkPanel.IsVisible)
            UpdateLinkResults(LinkSearch.Text);   // show the matches straight away
    }

    // The user typed in the link search box
    private void OnLinkSearchChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateLinkResults(e.NewTextValue);
    }

    // The user picked an existing note from the search results
    private void OnLinkResultChosen(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Note chosen) return;

        LinkResults.SelectedItem = null;
        InsertLink(chosen.Title);
    }

    // "Create ..." button: insert a link to a note that doesn't exist yet
    // (the note itself is only created later, when the link is tapped)
    private void OnLinkCreateClicked(object? sender, EventArgs e)
    {
        string title = (LinkSearch.Text ?? "").Trim();
        if (title.Length == 0) return;

        InsertLink(title);
    }

    // ---------- Link helpers ----------

    // Refills the "links in this note" list from the text currently in the editor
    private void RefreshLinks()
    {
        LinksList.ItemsSource = NoteLinks.FindLinks(TextEditor.Text);
    }

    // Updates the search results, and shows the "Create" button when nothing matches exactly
    private void UpdateLinkResults(string? query)
    {
        string q = (query ?? "").Trim();
        var allNotes = _storage.LoadAll();

        LinkResults.ItemsSource = NoteSearch.Find(allNotes, q);

        // Offer to create only when something is typed and no note has exactly that title
        LinkCreateButton.IsVisible = q.Length > 0 && NoteLinks.Resolve(q, allNotes) is null;
        LinkCreateButton.Text = $"Create \"{q}\"";
    }

    // Puts [[title]] into the text at the cursor, then closes the panel
    private void InsertLink(string title)
    {
        string link = $"[[{title}]]";
        string text = TextEditor.Text ?? "";

        // The cursor can be outside the text if the editor was never tapped, so keep it in range
        int position = Math.Clamp(TextEditor.CursorPosition, 0, text.Length);

        TextEditor.Text = text.Insert(position, link);          // triggers OnAnyBoxChanged (draft + links list)
        TextEditor.CursorPosition = position + link.Length;     // cursor just after the new link

        LinkSearch.Text = "";
        LinkPanel.IsVisible = false;
    }

    // ---------- Saving: boxes, draft, final file ----------

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

        RefreshLinks();   // OnAnyBoxChanged skipped the loading, so fill the links list here
    }

    // Copies the boxes into _note. Shared by all triggers; saves nothing.
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

    // Writes a draft file (the safety copy). Runs when the countdown ends.
    private void SaveDraft()
    {
        if (_note == null) return;

        CopyBoxesIntoNote();
        _storage.SaveDraft(_originalTitle, _note);
    }

    // Writes the final file, handles a rename, deletes the draft. Used by Save, by leaving, and by link taps.
    private void CommitNote()
    {
        if (_note == null) return;
        _draftTimer?.Stop();   // no stale draft should be written after this

        // Remove stray spaces from the title, so "  Run 5 " is saved as "Run 5".
        _note.Title = _note.Title.Trim();

        // Compare FILE NAMES, not titles: "my note" and "my-note" are the same file.
        bool nameChanged = FileNames.MakeSafe(_originalTitle) != FileNames.MakeSafe(_note.Title);

        // Only a renamed note can clash with a different note. If the name did not
        // change, the file existing is normal because it is this note's own file.
        if (nameChanged && _storage.TitleIsTaken(_note.Title))
        {
            _note.Title = _storage.UniqueTitle(_note.Title);   // e.g. "Run 5" -> "Run 5 2"
            TitleEntry.Text = _note.Title;                     // show the user the final name
        }

        // Save BEFORE deleting anything, so the note is never lost.
        _storage.Save(_note);

        if (nameChanged)
        {
            _storage.Delete(_originalTitle);
            _storage.UpdateLinksAfterRename(_originalTitle, _note.Title);   // remove the "//" from the comment
        }

        // The real file is safe, so the draft is no longer needed.
        _storage.DeleteDraft(_originalTitle);

        // The "original" is now exactly what we just saved. No UniqueTitle here,
        // because the file exists now and it would add " 2".
        _originalTitle = _note.Title;
    }

    // ---------- Small helpers ----------

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