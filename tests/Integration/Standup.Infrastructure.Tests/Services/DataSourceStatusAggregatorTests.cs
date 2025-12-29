// -----------------------------------------------------------------------
// <copyright file="DataSourceStatusAggregatorTests.cs" company="Standup">
//     Copyright (c) Standup. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Standup.Application.DTOs;
using Standup.Infrastructure.Services;
using Xunit;

namespace Standup.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="DataSourceStatusAggregator"/>.
/// </summary>
public sealed class DataSourceStatusAggregatorTests
{
    [Fact]
    public void Aggregate_WithEmptyList_ReturnsAllSuccess()
    {
        // Arrange
        var statuses = new List<DataSourceStatus>();

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.CommitsStatus.Should().Be(FetchStatus.Success);
        result.PullRequestsStatus.Should().Be(FetchStatus.Success);
        result.WorkItemsStatus.Should().Be(FetchStatus.Success);
    }

    [Fact]
    public void Aggregate_WithSingleStatus_ReturnsSameStatus()
    {
        // Arrange
        var status = new DataSourceStatus(
            FetchStatus.Success,
            FetchStatus.NoPat,
            FetchStatus.Error,
            CommitsError: null,
            PullRequestsError: "No PAT configured",
            WorkItemsError: "API unavailable");
        var statuses = new List<DataSourceStatus> { status };

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.Should().Be(status);
    }

    [Fact]
    public void Aggregate_WithAllSuccess_ReturnsSuccess()
    {
        // Arrange
        var statuses = new List<DataSourceStatus>
        {
            DataSourceStatus.AllSuccess(),
            DataSourceStatus.AllSuccess(),
            DataSourceStatus.AllSuccess(),
        };

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.CommitsStatus.Should().Be(FetchStatus.Success);
        result.PullRequestsStatus.Should().Be(FetchStatus.Success);
        result.WorkItemsStatus.Should().Be(FetchStatus.Success);
    }

    [Fact]
    public void Aggregate_WithMixedStatuses_ReturnsWorstForEachSource()
    {
        // Arrange
        var statuses = new List<DataSourceStatus>
        {
            new(FetchStatus.Success, FetchStatus.Success, FetchStatus.Success),
            new(FetchStatus.Error, FetchStatus.NoPat, FetchStatus.Success),
            new(FetchStatus.Success, FetchStatus.Success, FetchStatus.NotApplicable),
        };

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.CommitsStatus.Should().Be(FetchStatus.Error);
        result.PullRequestsStatus.Should().Be(FetchStatus.NoPat);
        result.WorkItemsStatus.Should().Be(FetchStatus.NotApplicable);
    }

    [Fact]
    public void Aggregate_PrioritizesErrorOverNoPat()
    {
        // Arrange
        var statuses = new List<DataSourceStatus>
        {
            new(FetchStatus.NoPat, FetchStatus.NoPat, FetchStatus.NoPat),
            new(FetchStatus.Error, FetchStatus.Success, FetchStatus.Success),
        };

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.CommitsStatus.Should().Be(FetchStatus.Error);
    }

    [Fact]
    public void Aggregate_PrioritizesNoPatOverNotApplicable()
    {
        // Arrange
        var statuses = new List<DataSourceStatus>
        {
            new(FetchStatus.NotApplicable, FetchStatus.Success, FetchStatus.Success),
            new(FetchStatus.NoPat, FetchStatus.Success, FetchStatus.Success),
        };

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.CommitsStatus.Should().Be(FetchStatus.NoPat);
    }

    [Fact]
    public void Aggregate_PrioritizesNotApplicableOverSuccess()
    {
        // Arrange
        var statuses = new List<DataSourceStatus>
        {
            new(FetchStatus.Success, FetchStatus.Success, FetchStatus.Success),
            new(FetchStatus.NotApplicable, FetchStatus.Success, FetchStatus.Success),
        };

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.CommitsStatus.Should().Be(FetchStatus.NotApplicable);
    }

    [Fact]
    public void Aggregate_AggregatesErrorMessages()
    {
        // Arrange
        var statuses = new List<DataSourceStatus>
        {
            new(
                FetchStatus.Error,
                FetchStatus.Success,
                FetchStatus.Success,
                CommitsError: "Repo 1 error"),
            new(
                FetchStatus.Error,
                FetchStatus.Success,
                FetchStatus.Success,
                CommitsError: "Repo 2 error"),
        };

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.CommitsError.Should().Contain("Repo 1 error");
        result.CommitsError.Should().Contain("Repo 2 error");
        result.CommitsError.Should().Contain(";");
    }

    [Fact]
    public void Aggregate_DeduplicatesErrorMessages()
    {
        // Arrange
        var statuses = new List<DataSourceStatus>
        {
            new(
                FetchStatus.Error,
                FetchStatus.Success,
                FetchStatus.Success,
                CommitsError: "Same error"),
            new(
                FetchStatus.Error,
                FetchStatus.Success,
                FetchStatus.Success,
                CommitsError: "Same error"),
        };

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.CommitsError.Should().Be("Same error");
    }

    [Fact]
    public void Aggregate_WithNullErrors_ReturnsNullError()
    {
        // Arrange
        var statuses = new List<DataSourceStatus>
        {
            new(FetchStatus.Success, FetchStatus.Success, FetchStatus.Success),
            new(FetchStatus.Success, FetchStatus.Success, FetchStatus.Success),
        };

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.CommitsError.Should().BeNull();
        result.PullRequestsError.Should().BeNull();
        result.WorkItemsError.Should().BeNull();
    }

    [Fact]
    public void Aggregate_AggregatesAllErrorTypes()
    {
        // Arrange
        var statuses = new List<DataSourceStatus>
        {
            new(
                FetchStatus.Error,
                FetchStatus.Error,
                FetchStatus.Error,
                CommitsError: "Commits failed",
                PullRequestsError: "PRs failed",
                WorkItemsError: "Work items failed"),
            new(FetchStatus.Success, FetchStatus.Success, FetchStatus.Success),
        };

        // Act
        var result = DataSourceStatusAggregator.Aggregate(statuses);

        // Assert
        result.CommitsError.Should().Be("Commits failed");
        result.PullRequestsError.Should().Be("PRs failed");
        result.WorkItemsError.Should().Be("Work items failed");
    }
}
