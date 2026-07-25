// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// How often a <see cref="PeriodicSchedule"/> recurs.
/// </summary>
public enum PeriodicFrequency
{
    /// <summary>Runs exactly once, at <see cref="PeriodicSchedule.StartTime"/> (or immediately if unset).</summary>
    Once,

    /// <summary>Runs every hour.</summary>
    Hourly,

    /// <summary>Runs every day, at <see cref="PeriodicSchedule.TimeOfDay"/>.</summary>
    Daily,

    /// <summary>Runs on the configured <see cref="PeriodicSchedule.DaysOfWeek"/>, at <see cref="PeriodicSchedule.TimeOfDay"/>.</summary>
    Weekly,

    /// <summary>Runs on the configured <see cref="PeriodicSchedule.DayOfMonth"/>, at <see cref="PeriodicSchedule.TimeOfDay"/>.</summary>
    Monthly,

    /// <summary>Runs every <see cref="PeriodicSchedule.Interval"/>.</summary>
    Custom,
}

/// <summary>
/// A set of days of the week. Unlike <see cref="DayOfWeek"/>, this is a <see cref="FlagsAttribute"/>
/// enum so more than one day can be combined, for example <c>WeekDays.Monday | WeekDays.Wednesday</c>.
/// </summary>
[Flags]
public enum WeekDays
{
    /// <summary>No day selected.</summary>
    None = 0,

    /// <summary>Monday.</summary>
    Monday = 1 << 0,

    /// <summary>Tuesday.</summary>
    Tuesday = 1 << 1,

    /// <summary>Wednesday.</summary>
    Wednesday = 1 << 2,

    /// <summary>Thursday.</summary>
    Thursday = 1 << 3,

    /// <summary>Friday.</summary>
    Friday = 1 << 4,

    /// <summary>Saturday.</summary>
    Saturday = 1 << 5,

    /// <summary>Sunday.</summary>
    Sunday = 1 << 6,

    /// <summary>Monday through Friday.</summary>
    Workdays = Monday | Tuesday | Wednesday | Thursday | Friday,

    /// <summary>Saturday and Sunday.</summary>
    Weekend = Saturday | Sunday,

    /// <summary>Every day of the week.</summary>
    All = Workdays | Weekend,
}

/// <summary>
/// Describes when and how often a <see cref="IWpfPeriodicService"/> runs: a start time, an
/// optional end time, a recurrence <see cref="Frequency"/>, and a human-readable
/// <see cref="Description"/>.
/// </summary>
/// <remarks>
/// <see cref="PeriodicFrequency.Daily"/>, <see cref="PeriodicFrequency.Weekly"/> and
/// <see cref="PeriodicFrequency.Monthly"/> are calendar-anchored: they run at a specific
/// wall-clock <see cref="TimeOfDay"/>, on specific day(s) of the week or a specific day of the
/// month - the same way a calendar reminder or a Windows Task Scheduler trigger would, not
/// simply "every N days from whenever the app happened to start". <see cref="PeriodicFrequency.Hourly"/>
/// and <see cref="PeriodicFrequency.Custom"/> are plain fixed-duration repeats instead.
/// </remarks>
public sealed class PeriodicSchedule
{
    /// <summary>How often the schedule recurs. Defaults to <see cref="PeriodicFrequency.Once"/>.</summary>
    public PeriodicFrequency Frequency { get; set; }

    /// <summary>
    /// The schedule produces no occurrence before this time. When <see langword="null"/>, the
    /// first occurrence is computed relative to "now" instead - for example, an
    /// <see cref="PeriodicFrequency.Hourly"/> schedule with no <see cref="StartTime"/> starts
    /// immediately, the same way a plain interval does.
    /// </summary>
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// The schedule produces no occurrence after this time. <see langword="null"/> (the default)
    /// means it never expires.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// The wall-clock time of day to run at. Used by <see cref="PeriodicFrequency.Daily"/>,
    /// <see cref="PeriodicFrequency.Weekly"/> and <see cref="PeriodicFrequency.Monthly"/>. When
    /// <see langword="null"/>, defaults to the time-of-day component of <see cref="StartTime"/>,
    /// or of "now" when <see cref="StartTime"/> is also unset.
    /// </summary>
    public TimeSpan? TimeOfDay { get; set; }

    /// <summary>
    /// The day(s) of the week to run on. Used by <see cref="PeriodicFrequency.Weekly"/>. When
    /// <see cref="Hosting.WeekDays.None"/> (the default), defaults to the day of week of
    /// <see cref="StartTime"/> (or of "now" when <see cref="StartTime"/> is also unset) - a
    /// single day, i.e. "every week on that day".
    /// </summary>
    public WeekDays DaysOfWeek { get; set; }

    /// <summary>
    /// The day of the month (1-31) to run on. Used by <see cref="PeriodicFrequency.Monthly"/>. A
    /// value past the end of a given month (for example 31 in February) is clamped to that
    /// month's last day. When <see langword="null"/>, defaults to the day of <see cref="StartTime"/>
    /// (or of "now" when <see cref="StartTime"/> is also unset).
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// The repeat interval. Required, and must be positive, when <see cref="Frequency"/> is
    /// <see cref="PeriodicFrequency.Custom"/>; ignored otherwise.
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>A human-readable description of what the service does, for display in a settings UI.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Validates that the schedule is internally consistent, throwing
    /// <see cref="InvalidOperationException"/> if it is not.
    /// </summary>
    public void Validate()
    {
        if (Frequency == PeriodicFrequency.Custom && (!Interval.HasValue || Interval.Value <= TimeSpan.Zero))
            throw new InvalidOperationException("A Custom schedule requires a positive Interval.");

        if (DayOfMonth is < 1 or > 31)
            throw new InvalidOperationException("DayOfMonth must be between 1 and 31.");

        if (StartTime.HasValue && EndTime.HasValue && StartTime.Value > EndTime.Value)
            throw new InvalidOperationException("StartTime must not be later than EndTime.");
    }

    /// <summary>
    /// Computes the next time this schedule should run, or <see langword="null"/> if it has no
    /// more occurrences - either <see cref="EndTime"/> has passed, or this is a
    /// <see cref="PeriodicFrequency.Once"/> schedule that has already run.
    /// </summary>
    /// <remarks>
    /// This is a pure function of its arguments (it never reads the system clock itself), so it
    /// can also be used to preview a schedule - for example from a settings UI, before saving it.
    /// If more than one occurrence was missed (the schedule was disabled, or the app was closed,
    /// across several periods), this returns only the next single occurrence rather than
    /// replaying a backlog.
    /// </remarks>
    /// <param name="now">The current time.</param>
    /// <param name="lastRun">The last time the schedule ran, or <see langword="null"/> if it never has.</param>
    public DateTimeOffset? GetNextOccurrence(DateTimeOffset now, DateTimeOffset? lastRun)
    {
        Validate();

        if (EndTime is { } expired && expired < now)
            return null;

        DateTimeOffset? candidate = Frequency switch
        {
            // Same "clamp to now" contract as NextFixed below - an overdue StartTime catches up
            // immediately rather than resolving to an instant that's already in the past.
            PeriodicFrequency.Once => lastRun is null ? (StartTime is { } start && start > now ? start : now) : null,
            PeriodicFrequency.Hourly => NextFixed(TimeSpan.FromHours(1), now, lastRun),
            PeriodicFrequency.Custom => NextFixed(Interval!.Value, now, lastRun),
            PeriodicFrequency.Daily => NextDaily(now, lastRun),
            PeriodicFrequency.Weekly => NextWeekly(now, lastRun),
            PeriodicFrequency.Monthly => NextMonthly(now, lastRun),
            _ => throw new InvalidOperationException($"Unknown frequency '{Frequency}'."),
        };

        if (candidate is null)
            return null;

        if (EndTime is { } end && candidate.Value > end)
            return null;

        return candidate;
    }

    /// <summary>Returns an independent copy of this schedule.</summary>
    public PeriodicSchedule Clone() => new()
    {
        Frequency = Frequency,
        StartTime = StartTime,
        EndTime = EndTime,
        TimeOfDay = TimeOfDay,
        DaysOfWeek = DaysOfWeek,
        DayOfMonth = DayOfMonth,
        Interval = Interval,
        Description = Description,
    };

    // Hourly/Custom are fixed-duration repeats, not wall-clock-anchored, so plain DateTimeOffset
    // arithmetic is both simpler and more correct than the local-time composition Daily/Weekly/
    // Monthly need below - a DateTimeOffset already denotes an absolute instant.
    DateTimeOffset NextFixed(TimeSpan period, DateTimeOffset now, DateTimeOffset? lastRun)
    {
        if (lastRun is null)
            return StartTime is { } start && start > now ? start : now;

        var next = lastRun.Value + period;
        if (next >= now)
            return next;

        // Catch up to a single next occurrence (do not replay every missed period) using exact
        // integer-tick ceiling division rather than TimeSpan/TimeSpan + Math.Ceiling, which can
        // overshoot by a whole period due to floating-point rounding.
        var elapsedTicks = (now - next).Ticks;
        var periodTicks = period.Ticks;
        var missedPeriods = (elapsedTicks + periodTicks - 1) / periodTicks;
        return next + TimeSpan.FromTicks(periodTicks * missedPeriods);
    }

    DateTimeOffset? NextDaily(DateTimeOffset now, DateTimeOffset? lastRun)
    {
        var nowLocal = now.LocalDateTime;
        var startOrNow = StartTime?.LocalDateTime ?? nowLocal;
        var timeOfDay = TimeOfDay ?? startOrNow.TimeOfDay;

        // Never scan forward from lastRun's own (possibly very stale) date - jump to the
        // neighborhood of "now" first, then a couple of days is always enough.
        var baseDate = lastRun is null
            ? (startOrNow.Date > nowLocal.Date ? startOrNow.Date : nowLocal.Date)
            : MaxDate(lastRun.Value.LocalDateTime.Date.AddDays(1), nowLocal.Date);

        for (var i = 0; i < 3; i++)
        {
            var candidate = Compose(baseDate.AddDays(i), timeOfDay);
            if (candidate >= now)
                return candidate;
        }

        return null; // Unreachable: i=1 always lands on or after "now".
    }

    DateTimeOffset? NextWeekly(DateTimeOffset now, DateTimeOffset? lastRun)
    {
        var nowLocal = now.LocalDateTime;
        var startOrNow = StartTime?.LocalDateTime ?? nowLocal;
        var timeOfDay = TimeOfDay ?? startOrNow.TimeOfDay;
        var days = DaysOfWeek == WeekDays.None ? ToWeekDays(startOrNow.DayOfWeek) : DaysOfWeek;

        var baseDate = lastRun is null
            ? (startOrNow.Date > nowLocal.Date ? startOrNow.Date : nowLocal.Date)
            : MaxDate(lastRun.Value.LocalDateTime.Date.AddDays(1), nowLocal.Date);

        // A full week plus one margin day always contains a matching day-of-week whose composed
        // instant is >= now (days is never empty - WeekDays.None is substituted above).
        for (var i = 0; i < 8; i++)
        {
            var date = baseDate.AddDays(i);
            if (!days.HasFlag(ToWeekDays(date.DayOfWeek)))
                continue;

            var candidate = Compose(date, timeOfDay);
            if (candidate >= now)
                return candidate;
        }

        return null; // Unreachable.
    }

    DateTimeOffset? NextMonthly(DateTimeOffset now, DateTimeOffset? lastRun)
    {
        var nowLocal = now.LocalDateTime;
        var startOrNow = StartTime?.LocalDateTime ?? nowLocal;
        var timeOfDay = TimeOfDay ?? startOrNow.TimeOfDay;
        // Kept separate from the (year, month) stepping below and re-clamped every iteration -
        // DateTime.AddMonths clamps internally, so chaining it would make e.g. day 31 silently
        // "stick" at 28 forever after the first February it steps through.
        var requestedDay = DayOfMonth ?? startOrNow.Day;

        var (earliestYear, earliestMonth) = lastRun is null
            ? (startOrNow.Year, startOrNow.Month)
            : AddMonths(lastRun.Value.LocalDateTime.Year, lastRun.Value.LocalDateTime.Month, 1);

        // As with Daily/Weekly, never anchor earlier than "now"'s month.
        var isEarliestBeforeNow = earliestYear < nowLocal.Year || (earliestYear == nowLocal.Year && earliestMonth < nowLocal.Month);
        var (year, month) = isEarliestBeforeNow ? (nowLocal.Year, nowLocal.Month) : (earliestYear, earliestMonth);

        for (var i = 0; i < 3; i++)
        {
            var (y, m) = AddMonths(year, month, i);
            var day = Math.Min(requestedDay, DateTime.DaysInMonth(y, m));
            var candidate = Compose(new DateTime(y, m, day), timeOfDay);
            if (candidate >= now)
                return candidate;
        }

        return null; // Unreachable.
    }

    // Composes a wall-clock instant from local date/time-of-day components and converts to
    // DateTimeOffset last, so the correct UTC offset is resolved fresh every time - this is what
    // keeps a "same wall-clock time" schedule correct across DST transitions instead of drifting
    // by an hour the way raw DateTimeOffset + TimeSpan arithmetic would.
    static DateTimeOffset Compose(DateTime localDate, TimeSpan timeOfDay) => new(localDate.Date + timeOfDay);

    static DateTime MaxDate(DateTime a, DateTime b) => a > b ? a : b;

    static (int Year, int Month) AddMonths(int year, int month, int delta)
    {
        var total = year * 12 + (month - 1) + delta;
        return (total / 12, total % 12 + 1);
    }

    // System.DayOfWeek is zero-based from Sunday; WeekDays is bit-0 from Monday - an explicit
    // mapping avoids silently misaligning every day by one via a bit-shift on the BCL ordinal.
    static WeekDays ToWeekDays(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => WeekDays.Monday,
        DayOfWeek.Tuesday => WeekDays.Tuesday,
        DayOfWeek.Wednesday => WeekDays.Wednesday,
        DayOfWeek.Thursday => WeekDays.Thursday,
        DayOfWeek.Friday => WeekDays.Friday,
        DayOfWeek.Saturday => WeekDays.Saturday,
        DayOfWeek.Sunday => WeekDays.Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek)),
    };
}
