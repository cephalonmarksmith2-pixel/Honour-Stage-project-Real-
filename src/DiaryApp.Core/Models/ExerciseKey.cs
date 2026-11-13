namespace DiaryApp.Models;

// Makes "Run", "RUN" and " run " count as the same exercise.
public static class ExerciseKey
{
    public static string From(string? exercise)
    {
        if (string.IsNullOrWhiteSpace(exercise))   // null, "" and "   " all mean "no exercise"
            return "unassigned";

        return exercise.Trim().ToLowerInvariant(); // ignore edge spaces and capitals
    }
}