using System.Diagnostics;

public static class TimeUtils {
    public static ulong GetNanoseconds() {
        double timestamp = Stopwatch.GetTimestamp();
        double nanoseconds = 1_000_000_000.0 * timestamp / Stopwatch.Frequency;
        return (ulong)nanoseconds;
    }
}