# Honour-Stage-project-Real-
Honour Stage project (Real)
## Testing

### Approach

The app's logic (parsing notes, building file names, searching, saving and loading) lives in a plain .NET
library, `DiaryApp.Core`. The phone app (`DiaryApp`) and the test project (`DiaryApp.Tests`) both use it.
Keeping the logic out of the Android project means the tests run on a laptop in about two seconds, with no
phone or emulator involved.

Tests are written with **xUnit**:

- `[Fact]` marks a test with one fixed input.
- `[Theory]` with `[InlineData(...)]` runs the same test once per row of inputs.
- Tests that write files use a **temporary folder** that is created for each test and deleted afterwards, so
  they never touch real notes.

For each feature, the expected answer is worked out **by hand first**, then the test is written, run, and
compared. If a test disagrees with the hand-worked answer, either the code or the reasoning has a bug.

### Running the tests

From the repository root:

```bash
# Run everything (builds only DiaryApp.Core and the tests, not the Android app)
dotnet test tests/DiaryApp.Tests

# Run one test class
dotnet test tests/DiaryApp.Tests --filter NoteSearchTests

# Run tests whose name contains some text
dotnet test tests/DiaryApp.Tests --filter "FullyQualifiedName~Numbers_Sort"
```

Running `dotnet test` from the repository root without the path also builds the Android app (about 30
seconds), so the path form above is used.

### Unit tests

#### File names (`FileNamesTests`)

Checks `FileNames.MakeSafe`, which turns a note title into a safe file name (spaces become dashes, forbidden
characters are removed, an empty result becomes `untitled`). The forbidden characters are a fixed list rather
than the operating system's own list, so behaviour is identical on Windows, Linux and Android.

| Input | Expected output |
|---|---|
| `Hill route` | `Hill-route` |
| `Run: hills/flat?` | `Run-hillsflat` |
| `a  b` (two spaces) | `a-b` |
| `???` | `untitled` |

#### Parsing (`NoteParserTests`)

Checks `NoteParser.Parse`, which reads the `[key:value]` tags and `[[links]]` from a note's text.

Input: `[exercise:run][date:2026-10-28][mood:happy] Great run today, see [[Hill route]].`

Expected: mood Happy, exercise `run`, date 28 October 2026, one link (`Hill route`), and the text with the tags
removed: `Great run today, see [[Hill route]].`

#### Searching and sorting (`NoteSearchTests`, 18 cases)

All filter tests use one fixed set of five notes, so the expected answers can be checked by hand:

| Title | Mood | Date |
|---|---|---|
| Hill route | Happy | 2026-10-28 |
| Flat route | Neutral | 2026-10-28 |
| Pool swim | Happy | 2026-10-27 |
| Sad day | Sad | 2026-10-27 |
| Hill sprint | Happy | 2026-10-26 |

| Area | What is checked |
|---|---|
| Sorting | Digits sort before letters, capitals are ignored (`2 miles`, `Apple`, `banana`, `zebra`). Numbers sort by value, so `Run 2` comes before `Run 10`. |
| Mood filter | Happy returns `Hill route`, `Hill sprint`, `Pool swim` (alphabetical). Counts per mood: Happy 3, Sad 1, Neutral 1. |
| Date filter | 28 Oct returns `Flat route`, `Hill route`. A date with no notes returns an empty list. |
| Combined filters | Text plus date, and text plus mood plus date, return only notes passing **all** filters. Text `pool` with mood Sad returns nothing, proving the filters combine with AND, not OR. |
| Blank search | An empty search, or one containing only spaces, returns every note. |
| Text search | `route` finds 2, `HILL` finds 1 (case does not matter), `zzz` finds 0 and returns an empty list (never null). |
| Sorted results | Search results are also sorted (`Route 2` before `Route 10`). |

#### Storage (`NoteStorageTests`)

Saves a note to a temporary folder, loads it back, and checks the title, mood, date and links survive. Also
checks the file is created with the safe name (`Hill-route.md`).

#### Drafts (`NoteStorageDraftTests`)

Unsaved edits are kept as `.draft` files so a crash or flat battery does not lose them.

- No draft: `HasDraft` is false and `LoadDraft` returns nothing.
- Saving a draft does **not** change the real note, and the draft does not appear in the list of notes.
- A draft stored under the original title keeps the edited title inside it.
- Deleting a draft removes it, and deleting twice does not fail.
- Saving over an existing note replaces it and leaves no temporary file behind (the safe-write step).

#### Creating notes (`NoteStorageCreateTests`)

Checks `UniqueTitle` and `CreateNew`, used by the add-note button.

- A free title is returned unchanged; `Untitled` becomes `Untitled 2`, then `Untitled 3`.
- Titles that map to the same file name (`my note` and `my-note`) count as taken.
- A blank base title falls back to `Untitled`.
- `CreateNew` saves a new empty note with a neutral mood, and calling it twice gives two different notes.

### Worked example: red, then green

The first parser test failed on its first run, which was expected because the `exercise` and `date` cases had
not been written yet:

```
Expected: "run"
Actual:   "Unassigned"
```

After implementing those two cases, the same test passed. A test stops at its first failing check, so fixing
one check revealed the next.

### Results

| Date | Tests run | Passed | Failed |
|---|---|---|---|
| 29 Oct 2026 | 24 | 24 | 0 |
| 1 Nov 2026 | 36 | 36 | 0 |

### Manual tests on a phone

The app is installed over USB (`./run.sh`, which builds, installs and launches it) and checked by hand.

| Test | Steps | Expected | Result |
|---|---|---|---|
| Live search | Type `hill`; type `POOL`; clear the box | 2 notes; 1 note; all 5 back, sorted | Passed |
| Open a note | Tap a note in the list | Editor opens with title, mood, date, exercise and text filled in | Passed |
| Save an edit | Change text and mood, tap Save | Returns to the list; reopening shows the change; still there after closing the app | Passed |
| Rename | Change a title and save | One note remains (no duplicate) | Passed |
| Blank title | Clear the title, tap Save | Pop-up asks for a title; nothing is saved | Passed |
| Draft written | Type in a note and wait two seconds | A `.md.draft` file appears beside the note (seen with `ls` on the phone's notes folder) | Passed |
| Draft recovery | Type, wait two seconds, then `adb shell am force-stop <app id>`; reopen the note | "Unsaved changes found" pop-up offers the draft or the saved version | Passed |
| Add note | Tap the "+" button | A new "Untitled" note opens; going straight back leaves no empty note | Passed |

### Known gaps

- Updating `[[links]]` in other notes when a title changes is not built or tested yet.
- Screen layout and navigation are only checked by hand; there are no automated user interface tests.
- Behaviour on Android's text sorting (the numeric ordering) was checked on the laptop; it should be spot-checked on
  the phone.

Coded by marksmith[run.sh](run.sh)