// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Core.UnitTests;

/// <summary>
/// Exercises <see cref="PeriodicSchedule.GetNextOccurrence"/> directly. It is a pure function of
/// its arguments (no system clock, no dispatcher/timer involved), which is exactly why every
/// calendar edge case below can be pinned down as an exact expected instant instead of pumping a
/// real <see cref="System.Windows.Threading.DispatcherTimer"/> - see <see cref="PeriodicServiceFeatureTests"/>
/// for the handful of tests that do need a real end-to-end timer fire.
/// </summary>
public class PeriodicScheduleTests
{
    // Fixed dates whose days of the week are used throughout: 2025-01-01 is a Wednesday, so
    // 2025-01-05/06/09/20 are Sun/Mon/Thu/Mon respectively.
    static DateTimeOffset Local(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local));

    // --- Once ---------------------------------------------------------------------------------

    [Fact]
    public void Once_FirstRun_NoStartTime_ReturnsNow()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Once };
        var now = Local(2025, 1, 15, 10, 30);

        Assert.Equal(now, schedule.GetNextOccurrence(now, lastRun: null));
    }

    [Fact]
    public void Once_FirstRun_StartTimeInFuture_ReturnsStartTime()
    {
        var start = Local(2025, 1, 20, 8, 0);
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Once, StartTime = start };
        var now = Local(2025, 1, 15, 10, 30);

        Assert.Equal(start, schedule.GetNextOccurrence(now, lastRun: null));
    }

    [Fact]
    public void Once_FirstRun_StartTimeInPast_CatchesUpToNow()
    {
        var start = Local(2025, 1, 1, 8, 0);
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Once, StartTime = start };
        var now = Local(2025, 1, 15, 10, 30);

        Assert.Equal(now, schedule.GetNextOccurrence(now, lastRun: null));
    }

    [Fact]
    public void Once_AlreadyRun_ReturnsNull()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Once };
        var now = Local(2025, 1, 15, 10, 30);
        var lastRun = Local(2025, 1, 10, 9, 0);

        Assert.Null(schedule.GetNextOccurrence(now, lastRun));
    }

    // --- Hourly / Custom (fixed-duration) ------------------------------------------------------

    [Fact]
    public void Hourly_FirstRun_NoStartTime_ReturnsNow()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Hourly };
        var now = Local(2025, 1, 15, 10, 30);

        Assert.Equal(now, schedule.GetNextOccurrence(now, lastRun: null));
    }

    [Fact]
    public void Hourly_StartTimeInFuture_DoesNotFireEarly()
    {
        var start = Local(2025, 1, 20, 0, 0);
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Hourly, StartTime = start };
        var now = Local(2025, 1, 15, 0, 0);

        Assert.Equal(start, schedule.GetNextOccurrence(now, lastRun: null));
    }

    [Fact]
    public void Hourly_SubsequentRun_StepsByOneHour()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Hourly };
        var lastRun = Local(2025, 1, 15, 9, 0);
        var now = Local(2025, 1, 15, 9, 5);

        Assert.Equal(Local(2025, 1, 15, 10, 0), schedule.GetNextOccurrence(now, lastRun));
    }

    [Fact]
    public void Hourly_StaleLastRun_CatchesUpToSingleNextOccurrence()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Hourly };
        var lastRun = Local(2020, 1, 1, 0, 0); // years stale, on the exact hour
        var now = Local(2025, 1, 15, 9, 37);

        // Must not replay years of missed hourly ticks - just the next on-the-hour instant >= now.
        Assert.Equal(Local(2025, 1, 15, 10, 0), schedule.GetNextOccurrence(now, lastRun));
    }

    [Fact]
    public void Custom_SubsequentRun_UsesConfiguredInterval()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Custom, Interval = TimeSpan.FromMinutes(15) };
        var lastRun = Local(2025, 1, 15, 9, 0);
        var now = Local(2025, 1, 15, 9, 1);

        Assert.Equal(Local(2025, 1, 15, 9, 15), schedule.GetNextOccurrence(now, lastRun));
    }

    [Fact]
    public void Custom_StaleLastRun_CatchesUpUsingExactCeilingDivision()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Custom, Interval = TimeSpan.FromMinutes(20) };
        var lastRun = Local(2025, 1, 15, 9, 0);
        var now = Local(2025, 1, 15, 10, 3); // 63 minutes later - not an exact multiple of 20

        // 9:00 -> 9:20 -> 9:40 -> 10:00 -> 10:20 is the first one >= 10:03.
        Assert.Equal(Local(2025, 1, 15, 10, 20), schedule.GetNextOccurrence(now, lastRun));
    }

    // --- Daily ----------------------------------------------------------------------------------

    [Fact]
    public void Daily_FirstRun_TimeOfDayStillAheadToday_ReturnsToday()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Daily, TimeOfDay = new TimeSpan(9, 0, 0) };
        var now = Local(2025, 1, 15, 8, 0);

        Assert.Equal(Local(2025, 1, 15, 9, 0), schedule.GetNextOccurrence(now, lastRun: null));
    }

    [Fact]
    public void Daily_FirstRun_TimeOfDayAlreadyPassedToday_RollsToTomorrow()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Daily, TimeOfDay = new TimeSpan(9, 0, 0) };
        var now = Local(2025, 1, 15, 10, 0);

        Assert.Equal(Local(2025, 1, 16, 9, 0), schedule.GetNextOccurrence(now, lastRun: null));
    }

    [Fact]
    public void Daily_SubsequentRun_ReturnsNextDaySameTime()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Daily, TimeOfDay = new TimeSpan(9, 0, 0) };
        var lastRun = Local(2025, 1, 15, 9, 0);
        var now = Local(2025, 1, 15, 9, 0, 5);

        Assert.Equal(Local(2025, 1, 16, 9, 0), schedule.GetNextOccurrence(now, lastRun));
    }

    [Fact]
    public void Daily_StartTimeInFuture_DoesNotFireEarly()
    {
        var start = Local(2025, 2, 1, 9, 0);
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Daily, StartTime = start };
        var now = Local(2025, 1, 15, 9, 0);

        Assert.Equal(Local(2025, 2, 1, 9, 0), schedule.GetNextOccurrence(now, lastRun: null));
    }

    // --- Weekly ---------------------------------------------------------------------------------

    [Fact]
    public void Weekly_DaysOfWeekNone_DefaultsToStartTimesDay()
    {
        var start = Local(2025, 1, 6, 9, 0); // a Monday
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Weekly, StartTime = start };

        Assert.Equal(start, schedule.GetNextOccurrence(start, lastRun: null));
    }

    [Fact]
    public void Weekly_MultipleDaysOfWeek_PicksEarliestMatchingDay()
    {
        var schedule = new PeriodicSchedule
        {
            Frequency = PeriodicFrequency.Weekly,
            DaysOfWeek = WeekDays.Monday | WeekDays.Thursday,
            TimeOfDay = new TimeSpan(9, 0, 0),
        };
        var now = Local(2025, 1, 6, 10, 0); // Monday, after 9am already passed

        // Monday's 9am has passed; the next match is Thursday 2025-01-09.
        Assert.Equal(Local(2025, 1, 9, 9, 0), schedule.GetNextOccurrence(now, lastRun: null));
    }

    [Fact]
    public void Weekly_DayOfWeekMapping_MatchesActualSunday()
    {
        // Regression test: System.DayOfWeek is zero-based from Sunday, WeekDays is bit-0 from
        // Monday. A naive bit-shift mapping would make this fire on Monday (2025-01-06) instead
        // of the requested Sunday (2025-01-05).
        var schedule = new PeriodicSchedule
        {
            Frequency = PeriodicFrequency.Weekly,
            DaysOfWeek = WeekDays.Sunday,
            TimeOfDay = new TimeSpan(9, 0, 0),
        };
        var now = Local(2025, 1, 1, 0, 0); // a Wednesday

        Assert.Equal(Local(2025, 1, 5, 9, 0), schedule.GetNextOccurrence(now, lastRun: null));
    }

    [Fact]
    public void Weekly_StaleLastRun_ReturnsNearFutureOccurrence()
    {
        // Regression test: scanning day-by-day starting from lastRun's own (years-old) date must
        // not time out (or worse, incorrectly return null and permanently "complete" the entry).
        var schedule = new PeriodicSchedule
        {
            Frequency = PeriodicFrequency.Weekly,
            DaysOfWeek = WeekDays.Monday,
            TimeOfDay = new TimeSpan(9, 0, 0),
        };
        var lastRun = Local(2020, 1, 6, 9, 0); // years stale
        var now = Local(2025, 1, 15, 12, 0); // a Wednesday

        Assert.Equal(Local(2025, 1, 20, 9, 0), schedule.GetNextOccurrence(now, lastRun));
    }

    // --- Monthly --------------------------------------------------------------------------------

    [Fact]
    public void Monthly_DayOfMonth31_ClampsInFebruaryThenRestoresInMarch()
    {
        // Regression test: DateTime.AddMonths clamps internally, so chaining it off an
        // already-clamped date would make day 31 silently "stick" at 28 after February.
        var schedule = new PeriodicSchedule
        {
            Frequency = PeriodicFrequency.Monthly,
            DayOfMonth = 31,
            TimeOfDay = new TimeSpan(9, 0, 0),
        };

        var january = schedule.GetNextOccurrence(Local(2025, 1, 1), lastRun: null);
        Assert.Equal(Local(2025, 1, 31, 9, 0), january);

        var february = schedule.GetNextOccurrence(Local(2025, 2, 1), january);
        Assert.Equal(Local(2025, 2, 28, 9, 0), february); // 2025 is not a leap year

        var march = schedule.GetNextOccurrence(Local(2025, 3, 1), february);
        Assert.Equal(Local(2025, 3, 31, 9, 0), march); // back to 31, not stuck at 28
    }

    [Fact]
    public void Monthly_StaleLastRun_ReturnsNearFutureOccurrence()
    {
        var schedule = new PeriodicSchedule
        {
            Frequency = PeriodicFrequency.Monthly,
            DayOfMonth = 15,
            TimeOfDay = new TimeSpan(9, 0, 0),
        };
        var lastRun = Local(2020, 1, 15, 9, 0); // years stale
        var now = Local(2025, 6, 20, 0, 0); // after the 15th this month

        Assert.Equal(Local(2025, 7, 15, 9, 0), schedule.GetNextOccurrence(now, lastRun));
    }

    [Fact]
    public void Monthly_StartTimeInFuture_DoesNotFireEarly()
    {
        var start = Local(2025, 6, 10, 9, 0);
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Monthly, StartTime = start };
        var now = Local(2025, 1, 15, 0, 0);

        Assert.Equal(start, schedule.GetNextOccurrence(now, lastRun: null));
    }

    // --- EndTime ---------------------------------------------------------------------------------

    [Fact]
    public void EndTime_NextCandidateWouldExceedIt_ReturnsNull()
    {
        var schedule = new PeriodicSchedule
        {
            Frequency = PeriodicFrequency.Daily,
            TimeOfDay = new TimeSpan(9, 0, 0),
            EndTime = Local(2025, 1, 15, 23, 59, 59),
        };
        var lastRun = Local(2025, 1, 15, 9, 0);
        var now = Local(2025, 1, 15, 10, 0);

        Assert.Null(schedule.GetNextOccurrence(now, lastRun));
    }

    [Fact]
    public void EndTime_AlreadyPassed_ReturnsNull()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Hourly, EndTime = Local(2025, 1, 1) };
        var now = Local(2025, 1, 15, 10, 0);

        Assert.Null(schedule.GetNextOccurrence(now, lastRun: null));
    }

    [Fact]
    public void EndTime_IsInclusive_CandidateEqualToEndTimeStillFires()
    {
        var end = Local(2025, 1, 15, 9, 0);
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Once, StartTime = end, EndTime = end };
        var now = Local(2025, 1, 10);

        Assert.Equal(end, schedule.GetNextOccurrence(now, lastRun: null));
    }

    // --- Validate -------------------------------------------------------------------------------

    [Fact]
    public void Validate_DefaultSchedule_DoesNotThrow()
    {
        var schedule = new PeriodicSchedule();

        Assert.Equal(PeriodicFrequency.Once, schedule.Frequency);
        var exception = Record.Exception(schedule.Validate);
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Validate_CustomWithNonPositiveInterval_Throws(double seconds)
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Custom, Interval = TimeSpan.FromSeconds(seconds) };

        Assert.Throws<InvalidOperationException>(schedule.Validate);
    }

    [Fact]
    public void Validate_CustomWithoutInterval_Throws()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Custom };

        Assert.Throws<InvalidOperationException>(schedule.Validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Validate_DayOfMonthOutOfRange_Throws(int dayOfMonth)
    {
        var schedule = new PeriodicSchedule { DayOfMonth = dayOfMonth };

        Assert.Throws<InvalidOperationException>(schedule.Validate);
    }

    [Fact]
    public void Validate_StartTimeAfterEndTime_Throws()
    {
        var schedule = new PeriodicSchedule
        {
            StartTime = Local(2025, 2, 1),
            EndTime = Local(2025, 1, 1),
        };

        Assert.Throws<InvalidOperationException>(schedule.Validate);
    }

    [Fact]
    public void GetNextOccurrence_InvalidSchedule_Throws()
    {
        var schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Custom };

        Assert.Throws<InvalidOperationException>(() => schedule.GetNextOccurrence(DateTimeOffset.Now, null));
    }

    // --- Clone ----------------------------------------------------------------------------------

    [Fact]
    public void Clone_CopiesEveryFieldAndIsIndependent()
    {
        var original = new PeriodicSchedule
        {
            Frequency = PeriodicFrequency.Weekly,
            StartTime = Local(2025, 1, 1),
            EndTime = Local(2025, 12, 31),
            TimeOfDay = new TimeSpan(9, 0, 0),
            DaysOfWeek = WeekDays.Monday | WeekDays.Friday,
            DayOfMonth = 15,
            Interval = TimeSpan.FromMinutes(30),
            Description = "Original",
        };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(original.Frequency, clone.Frequency);
        Assert.Equal(original.StartTime, clone.StartTime);
        Assert.Equal(original.EndTime, clone.EndTime);
        Assert.Equal(original.TimeOfDay, clone.TimeOfDay);
        Assert.Equal(original.DaysOfWeek, clone.DaysOfWeek);
        Assert.Equal(original.DayOfMonth, clone.DayOfMonth);
        Assert.Equal(original.Interval, clone.Interval);
        Assert.Equal(original.Description, clone.Description);

        clone.Description = "Changed";
        Assert.Equal("Original", original.Description);
    }
}
