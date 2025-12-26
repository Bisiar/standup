using FluentAssertions;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using Xunit;

namespace Standup.Domain.Tests.Interfaces;

public class SummaryOptionsTests
{
    [Fact]
    public void SummaryOptions_HasDefaultValues()
    {
        // Act
        var options = new SummaryOptions();

        // Assert
        options.Type.Should().Be(SummaryType.Technical);
        options.CustomPrompt.Should().BeNull();
        options.IncludeBlockers.Should().BeTrue();
        options.IncludeNextSteps.Should().BeTrue();
        options.Tone.Should().Be(SummaryTone.Professional);
        options.MaxLength.Should().Be(500);
    }

    [Fact]
    public void SummaryOptions_CanSetCustomValues()
    {
        // Act
        var options = new SummaryOptions(
            Type: SummaryType.Executive,
            CustomPrompt: "Focus on business value",
            IncludeBlockers: false,
            IncludeNextSteps: false,
            Tone: SummaryTone.Casual,
            MaxLength: 250);

        // Assert
        options.Type.Should().Be(SummaryType.Executive);
        options.CustomPrompt.Should().Be("Focus on business value");
        options.IncludeBlockers.Should().BeFalse();
        options.IncludeNextSteps.Should().BeFalse();
        options.Tone.Should().Be(SummaryTone.Casual);
        options.MaxLength.Should().Be(250);
    }

    [Fact]
    public void SummaryOptions_IsRecord_SupportsEquality()
    {
        // Arrange
        var options1 = new SummaryOptions(
            Type: SummaryType.CodeReview,
            Tone: SummaryTone.Brief,
            MaxLength: 300);

        var options2 = new SummaryOptions(
            Type: SummaryType.CodeReview,
            Tone: SummaryTone.Brief,
            MaxLength: 300);

        // Assert
        options1.Should().Be(options2);
        (options1 == options2).Should().BeTrue();
    }

    [Fact]
    public void SummaryOptions_DifferentValues_AreNotEqual()
    {
        // Arrange
        var options1 = new SummaryOptions(Type: SummaryType.Technical);
        var options2 = new SummaryOptions(Type: SummaryType.Executive);

        // Assert
        options1.Should().NotBe(options2);
        (options1 != options2).Should().BeTrue();
    }

    [Fact]
    public void SummaryOptions_SupportsDeconstruction()
    {
        // Arrange
        var options = new SummaryOptions(
            Type: SummaryType.CodeReview,
            CustomPrompt: "Custom instructions",
            IncludeBlockers: true,
            IncludeNextSteps: false,
            Tone: SummaryTone.Casual,
            MaxLength: 400);

        // Act
        var (type, customPrompt, includeBlockers, includeNextSteps, tone, maxLength) = options;

        // Assert
        type.Should().Be(SummaryType.CodeReview);
        customPrompt.Should().Be("Custom instructions");
        includeBlockers.Should().BeTrue();
        includeNextSteps.Should().BeFalse();
        tone.Should().Be(SummaryTone.Casual);
        maxLength.Should().Be(400);
    }

    [Theory]
    [InlineData(SummaryType.Technical)]
    [InlineData(SummaryType.Executive)]
    [InlineData(SummaryType.CodeReview)]
    public void SummaryOptions_SupportsDifferentSummaryTypes(SummaryType summaryType)
    {
        // Act
        var options = new SummaryOptions(Type: summaryType);

        // Assert
        options.Type.Should().Be(summaryType);
    }

    [Theory]
    [InlineData(SummaryTone.Professional)]
    [InlineData(SummaryTone.Casual)]
    [InlineData(SummaryTone.Brief)]
    public void SummaryOptions_SupportsDifferentTones(SummaryTone tone)
    {
        // Act
        var options = new SummaryOptions(Tone: tone);

        // Assert
        options.Tone.Should().Be(tone);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(250)]
    [InlineData(500)]
    [InlineData(1000)]
    public void SummaryOptions_SupportsDifferentMaxLengths(int maxLength)
    {
        // Act
        var options = new SummaryOptions(MaxLength: maxLength);

        // Assert
        options.MaxLength.Should().Be(maxLength);
    }

    [Fact]
    public void SummaryOptions_WithCustomPrompt_StoresPrompt()
    {
        // Act
        var options = new SummaryOptions(
            CustomPrompt: "Emphasize cost savings and ROI. Keep it brief and focused on executive priorities.");

        // Assert
        options.CustomPrompt.Should().Contain("cost savings");
        options.CustomPrompt.Should().Contain("executive priorities");
    }

    [Fact]
    public void SummaryOptions_WithBlockersDisabled_DoesNotIncludeBlockers()
    {
        // Act
        var options = new SummaryOptions(IncludeBlockers: false);

        // Assert
        options.IncludeBlockers.Should().BeFalse();
    }

    [Fact]
    public void SummaryOptions_WithNextStepsDisabled_DoesNotIncludeNextSteps()
    {
        // Act
        var options = new SummaryOptions(IncludeNextSteps: false);

        // Assert
        options.IncludeNextSteps.Should().BeFalse();
    }

    [Fact]
    public void SummaryOptions_SupportsWithExpression()
    {
        // Arrange
        var original = new SummaryOptions(
            Type: SummaryType.Technical,
            Tone: SummaryTone.Professional,
            MaxLength: 500);

        // Act
        var updated = original with { Type = SummaryType.Executive, MaxLength = 200 };

        // Assert
        updated.Type.Should().Be(SummaryType.Executive);
        updated.MaxLength.Should().Be(200);
        updated.Tone.Should().Be(original.Tone);
        original.Type.Should().Be(SummaryType.Technical); // Original unchanged
        original.MaxLength.Should().Be(500);
    }

    [Fact]
    public void SummaryOptions_ForExecutive_UsesAppropriateDefaults()
    {
        // Act
        var options = new SummaryOptions(
            Type: SummaryType.Executive,
            MaxLength: 200);

        // Assert
        options.Type.Should().Be(SummaryType.Executive);
        options.MaxLength.Should().Be(200);
        options.Tone.Should().Be(SummaryTone.Professional);
    }

    [Fact]
    public void SummaryOptions_ForTechnical_UsesDefaultMaxLength()
    {
        // Act
        var options = new SummaryOptions(Type: SummaryType.Technical);

        // Assert
        options.Type.Should().Be(SummaryType.Technical);
        options.MaxLength.Should().Be(500);
    }

    [Fact]
    public void SummaryOptions_WithAllFeaturesEnabled_IncludesEverything()
    {
        // Act
        var options = new SummaryOptions(
            IncludeBlockers: true,
            IncludeNextSteps: true);

        // Assert
        options.IncludeBlockers.Should().BeTrue();
        options.IncludeNextSteps.Should().BeTrue();
    }
}
