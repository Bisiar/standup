using FluentAssertions;
using Standup.Domain.ValueObjects;
using Xunit;

namespace Standup.Domain.Tests.ValueObjects;

public sealed class DateRangeTests
{
    [Fact]
    public void Duration_ReturnsCorrectTimeSpan()
    {
        // Arrange
        var start = new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 15, 17, 0, 0, TimeSpan.Zero);
        var range = new DateRange(start, end);

        // Act
        var duration = range.Duration;

        // Assert
        duration.Should().Be(TimeSpan.FromHours(8));
    }

    [Fact]
    public void LastNHours_ReturnsCorrectRange()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var range = DateRange.LastNHours(24);
        var after = DateTimeOffset.UtcNow;

        // Assert
        range.Duration.Should().BeCloseTo(TimeSpan.FromHours(24), TimeSpan.FromSeconds(1));
        range.End.Should().BeOnOrAfter(before);
        range.End.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void LastNHours_WithDifferentHours_ReturnsCorrectDuration()
    {
        // Arrange & Act
        var range8 = DateRange.LastNHours(8);
        var range12 = DateRange.LastNHours(12);
        var range48 = DateRange.LastNHours(48);

        // Assert
        range8.Duration.Should().BeCloseTo(TimeSpan.FromHours(8), TimeSpan.FromSeconds(1));
        range12.Duration.Should().BeCloseTo(TimeSpan.FromHours(12), TimeSpan.FromSeconds(1));
        range48.Duration.Should().BeCloseTo(TimeSpan.FromHours(48), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DateRange_IsRecord_SupportsEquality()
    {
        // Arrange
        var start = new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 15, 17, 0, 0, TimeSpan.Zero);

        var range1 = new DateRange(start, end);
        var range2 = new DateRange(start, end);

        // Assert
        range1.Should().Be(range2);
        (range1 == range2).Should().BeTrue();
    }

    [Fact]
    public void DateRange_DifferentValues_AreNotEqual()
    {
        // Arrange
        var start1 = new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero);
        var start2 = new DateTimeOffset(2024, 1, 16, 9, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 17, 17, 0, 0, TimeSpan.Zero);

        var range1 = new DateRange(start1, end);
        var range2 = new DateRange(start2, end);

        // Assert
        range1.Should().NotBe(range2);
        (range1 != range2).Should().BeTrue();
    }

    [Fact]
    public void DateRange_SupportsDeconstruction()
    {
        // Arrange
        var start = new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 15, 17, 0, 0, TimeSpan.Zero);
        var range = new DateRange(start, end);

        // Act
        var (deconstructedStart, deconstructedEnd) = range;

        // Assert
        deconstructedStart.Should().Be(start);
        deconstructedEnd.Should().Be(end);
    }
}
