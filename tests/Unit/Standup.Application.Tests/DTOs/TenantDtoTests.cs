using FluentAssertions;
using Standup.Application.DTOs;
using Xunit;

namespace Standup.Application.Tests.DTOs;

public class TenantDtoTests
{
    [Fact]
    public void TenantDto_CanBeCreated()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var dto = new TenantDto(
            Id: "tenant-123",
            Name: "Contoso Corp",
            IsActive: true,
            CreatedAt: now);

        // Assert
        dto.Id.Should().Be("tenant-123");
        dto.Name.Should().Be("Contoso Corp");
        dto.IsActive.Should().BeTrue();
        dto.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void TenantDto_IsRecord_SupportsEquality()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var dto1 = new TenantDto("tenant-1", "Name", true, now);
        var dto2 = new TenantDto("tenant-1", "Name", true, now);

        // Act & Assert
        dto1.Should().Be(dto2);
        (dto1 == dto2).Should().BeTrue();
    }

    [Fact]
    public void TenantDto_DifferentValues_AreNotEqual()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var dto1 = new TenantDto("tenant-1", "Name A", true, now);
        var dto2 = new TenantDto("tenant-1", "Name B", true, now);

        // Act & Assert
        dto1.Should().NotBe(dto2);
    }

    [Fact]
    public void TenantDto_SupportsDeconstruction()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var dto = new TenantDto("tenant-1", "Contoso", true, now);

        // Act
        var (id, name, isActive, createdAt) = dto;

        // Assert
        id.Should().Be("tenant-1");
        name.Should().Be("Contoso");
        isActive.Should().BeTrue();
        createdAt.Should().Be(now);
    }

    [Fact]
    public void TenantDto_WithInactiveTenant_HasIsActiveFalse()
    {
        // Arrange & Act
        var dto = new TenantDto(
            Id: "tenant-inactive",
            Name: "Inactive Tenant",
            IsActive: false,
            CreatedAt: DateTimeOffset.UtcNow);

        // Assert
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void TenantDto_SupportsWithExpression()
    {
        // Arrange
        var original = new TenantDto("tenant-1", "Original", true, DateTimeOffset.UtcNow);

        // Act
        var modified = original with { Name = "Modified" };

        // Assert
        modified.Name.Should().Be("Modified");
        modified.Id.Should().Be(original.Id);
        modified.IsActive.Should().Be(original.IsActive);
        modified.CreatedAt.Should().Be(original.CreatedAt);
    }
}
