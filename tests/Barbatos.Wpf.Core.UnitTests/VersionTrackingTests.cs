// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;

namespace Barbatos.Wpf.Core.UnitTests;

// VersionTracking.Default is a process-wide singleton whose history persists across test
// runs on disk (that persistence is the entire point of the feature), so these tests assert
// invariants that hold regardless of prior runs rather than exact "first launch" booleans.
public class VersionTrackingTests
{
    [Fact]
    public void DefaultIsCached()
    {
        Assert.NotNull(VersionTracking.Default);
        Assert.Same(VersionTracking.Default, VersionTracking.Default);
    }

    [Fact]
    public void CurrentVersionAndBuildMatchAppInfo()
    {
        Assert.Equal(AppInfo.VersionString, VersionTracking.CurrentVersion);
        Assert.Equal(AppInfo.BuildString, VersionTracking.CurrentBuild);
    }

    [Fact]
    public void VersionHistoryAlwaysContainsTheCurrentVersion()
    {
        Assert.Contains(VersionTracking.CurrentVersion, VersionTracking.VersionHistory);
    }

    [Fact]
    public void BuildHistoryAlwaysContainsTheCurrentBuild()
    {
        Assert.Contains(VersionTracking.CurrentBuild, VersionTracking.BuildHistory);
    }

    [Fact]
    public void IsFirstLaunchForVersionMatchesTheCurrentVersionFlag()
    {
        Assert.Equal(VersionTracking.IsFirstLaunchForCurrentVersion, VersionTracking.IsFirstLaunchForVersion(VersionTracking.CurrentVersion));
    }

    [Fact]
    public void IsFirstLaunchForVersionIsFalseForAnUnrelatedVersion()
    {
        Assert.False(VersionTracking.IsFirstLaunchForVersion("999.999.999.999-never-used"));
    }

    [Fact]
    public void IsFirstLaunchForBuildIsFalseForAnUnrelatedBuild()
    {
        Assert.False(VersionTracking.IsFirstLaunchForBuild("never-used-build"));
    }

    [Fact]
    public void TrackIsIdempotent()
    {
        var versionHistoryBefore = VersionTracking.VersionHistory.ToArray();

        VersionTracking.Track();
        VersionTracking.Track();

        Assert.Equal(versionHistoryBefore, VersionTracking.VersionHistory);
    }

    [Fact]
    public void FirstInstalledVersionIsTheStartOfTheHistory()
    {
        Assert.Equal(VersionTracking.VersionHistory.First(), VersionTracking.FirstInstalledVersion);
    }
}
