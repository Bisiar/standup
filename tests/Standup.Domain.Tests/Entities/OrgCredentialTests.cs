using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Domain.Tests.Entities;

public class OrgCredentialTests
{
    [Fact]
    public void NewOrgCredential_HasDefaultValues()
    {
        // Act
        var credential = new OrgCredential();

        // Assert
        credential.Id.Should().NotBeNullOrEmpty();
        credential.SourceType.Should().Be(SourceType.GitHub);
        credential.Organization.Should().BeEmpty();
        credential.EncryptedPat.Should().BeEmpty();
    }

    [Fact]
    public void OrgCredential_GeneratesUniqueIds()
    {
        // Act
        var credential1 = new OrgCredential();
        var credential2 = new OrgCredential();

        // Assert
        credential1.Id.Should().NotBe(credential2.Id);
    }

    [Fact]
    public void OrgCredential_CanSetProperties()
    {
        // Arrange
        var credential = new OrgCredential();

        // Act
        credential.SourceType = SourceType.AzureDevOps;
        credential.Organization = "journeyteam";
        credential.EncryptedPat = "encrypted_pat_value_here";

        // Assert
        credential.SourceType.Should().Be(SourceType.AzureDevOps);
        credential.Organization.Should().Be("journeyteam");
        credential.EncryptedPat.Should().Be("encrypted_pat_value_here");
    }

    [Theory]
    [InlineData(SourceType.GitHub)]
    [InlineData(SourceType.AzureDevOps)]
    public void OrgCredential_SupportsDifferentSourceTypes(SourceType sourceType)
    {
        // Arrange
        var credential = new OrgCredential();

        // Act
        credential.SourceType = sourceType;

        // Assert
        credential.SourceType.Should().Be(sourceType);
    }

    [Fact]
    public void OrgCredential_ForGitHub_StoresOrgName()
    {
        // Arrange
        var credential = new OrgCredential();

        // Act
        credential.SourceType = SourceType.GitHub;
        credential.Organization = "microsoft";
        credential.EncryptedPat = "github_encrypted_pat";

        // Assert
        credential.SourceType.Should().Be(SourceType.GitHub);
        credential.Organization.Should().Be("microsoft");
        credential.EncryptedPat.Should().Be("github_encrypted_pat");
    }

    [Fact]
    public void OrgCredential_ForAzureDevOps_StoresOrgName()
    {
        // Arrange
        var credential = new OrgCredential();

        // Act
        credential.SourceType = SourceType.AzureDevOps;
        credential.Organization = "contoso";
        credential.EncryptedPat = "azdo_encrypted_pat";

        // Assert
        credential.SourceType.Should().Be(SourceType.AzureDevOps);
        credential.Organization.Should().Be("contoso");
        credential.EncryptedPat.Should().Be("azdo_encrypted_pat");
    }

    [Fact]
    public void OrgCredential_WithEncryptedPat_StoresEncryptedValue()
    {
        // Arrange
        var credential = new OrgCredential();
        var encryptedValue = "AQAAAAIAAACGAAAAEAAAAJxK7VZ..."; // Simulated encrypted value

        // Act
        credential.EncryptedPat = encryptedValue;

        // Assert
        credential.EncryptedPat.Should().Be(encryptedValue);
    }

    [Fact]
    public void OrgCredential_CanBeUsedForPATInheritance()
    {
        // Arrange - Simulate org-level credential
        var orgCredential = new OrgCredential
        {
            SourceType = SourceType.GitHub,
            Organization = "my-org",
            EncryptedPat = "org_level_encrypted_pat"
        };

        // Assert - Repos in "my-org" can use this credential
        orgCredential.Organization.Should().Be("my-org");
        orgCredential.EncryptedPat.Should().NotBeEmpty();
    }

    [Fact]
    public void OrgCredential_WithEmptyOrganization_CanBeSet()
    {
        // Arrange
        var credential = new OrgCredential();

        // Act
        credential.Organization = string.Empty;

        // Assert
        credential.Organization.Should().BeEmpty();
    }

    [Fact]
    public void OrgCredential_WithEmptyEncryptedPat_CanBeSet()
    {
        // Arrange
        var credential = new OrgCredential();

        // Act
        credential.EncryptedPat = string.Empty;

        // Assert
        credential.EncryptedPat.Should().BeEmpty();
    }

    [Fact]
    public void OrgCredential_IdIsGuid_WhenGenerated()
    {
        // Act
        var credential = new OrgCredential();

        // Assert
        Guid.TryParse(credential.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void OrgCredential_SupportsMultipleOrganizations()
    {
        // Arrange
        var credential1 = new OrgCredential
        {
            SourceType = SourceType.GitHub,
            Organization = "org1",
            EncryptedPat = "pat1"
        };

        var credential2 = new OrgCredential
        {
            SourceType = SourceType.GitHub,
            Organization = "org2",
            EncryptedPat = "pat2"
        };

        // Assert
        credential1.Organization.Should().NotBe(credential2.Organization);
        credential1.EncryptedPat.Should().NotBe(credential2.EncryptedPat);
    }

    [Fact]
    public void OrgCredential_CanUpdateEncryptedPat()
    {
        // Arrange
        var credential = new OrgCredential
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "test-org",
            EncryptedPat = "old_encrypted_pat"
        };

        // Act
        credential.EncryptedPat = "new_encrypted_pat";

        // Assert
        credential.EncryptedPat.Should().Be("new_encrypted_pat");
    }
}
