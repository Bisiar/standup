# MAUI Secure Storage

This document explains how Standup.Maui uses secure storage for persisting sensitive configuration data like API keys and Personal Access Tokens (PATs).

## Overview

The app uses `Microsoft.Maui.Storage.SecureStorage` to store project configurations securely. SecureStorage provides platform-specific secure storage mechanisms:

| Platform | Storage Mechanism |
|----------|-------------------|
| macOS/MacCatalyst | Keychain Services |
| Windows | Windows Credential Manager (DPAPI) |
| iOS | Keychain Services |
| Android | Android Keystore + SharedPreferences |

## Implementation

### Storage Keys

The app uses two storage keys defined in `ProjectService.cs`:

```csharp
private const string ProjectsKey = "standup_projects";      // JSON array of all projects
private const string CurrentProjectKey = "standup_current_project";  // ID of active project
```

### Data Structure

Projects are serialized as JSON and stored in SecureStorage:

```csharp
public class ProjectInstance
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? TenantName { get; set; }
    public string ApiEndpoint { get; set; }
    public bool IsDefault { get; set; }

    // Source configuration (contains sensitive PAT)
    public bool UseLocalGeneration { get; set; }
    public SourceType SourceType { get; set; }
    public string? SourceOrganization { get; set; }
    public string? SourceProject { get; set; }
    public string? SourceRepository { get; set; }
    public string? SourcePat { get; set; }  // Personal Access Token - stored encrypted
    public string? AuthorIdentifier { get; set; }
}
```

### Read/Write Operations

```csharp
// Reading from SecureStorage
var json = await SecureStorage.Default.GetAsync(ProjectsKey);
var projects = JsonSerializer.Deserialize<List<ProjectInstance>>(json);

// Writing to SecureStorage
var json = JsonSerializer.Serialize(projects);
await SecureStorage.Default.SetAsync(ProjectsKey, json);
```

## MacCatalyst Configuration

### Entitlements

For SecureStorage to work on macOS/MacCatalyst, the app requires proper entitlements. Located at `Platforms/MacCatalyst/Entitlements.plist`:

**Development (without provisioning profile):**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- App Sandbox disabled for development -->
    <key>com.apple.security.app-sandbox</key>
    <false/>
</dict>
</plist>
```

**Production (with provisioning profile):**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.app-sandbox</key>
    <true/>
    <key>keychain-access-groups</key>
    <array>
        <string>$(AppIdentifierPrefix)com.journeyteam.standup</string>
    </array>
</dict>
</plist>
```

### Project File Configuration

The entitlements file is referenced in `Standup.Maui.csproj`:

```xml
<PropertyGroup>
    <!-- Enable Keychain entitlements for SecureStorage on MacCatalyst -->
    <CodesignEntitlements Condition="$(TargetFramework.Contains('maccatalyst'))">
        Platforms\MacCatalyst\Entitlements.plist
    </CodesignEntitlements>
</PropertyGroup>
```

## Common Errors

### MissingEntitlement Error

**Symptom:** `Error adding record: MissingEntitlement`

**Cause:** The app is trying to access Keychain without proper entitlements.

**Solutions:**

1. **Development:** Disable App Sandbox in `Entitlements.plist`:
   ```xml
   <key>com.apple.security.app-sandbox</key>
   <false/>
   ```

2. **Production:** Configure a valid provisioning profile with Keychain access groups.

### Provisioning Profile Warnings

**Symptom:**
```
Cannot expand $(AppIdentifierPrefix) in Entitlements.plist without a provisioning profile
```

**Cause:** Using `keychain-access-groups` without a provisioning profile.

**Solution:** For development, use the sandbox-disabled approach. For production, set up an Apple Developer account and provisioning profile.

## Security Considerations

1. **PAT Storage:** Personal Access Tokens are stored in SecureStorage which encrypts data at rest using platform-native mechanisms.

2. **Additional Encryption:** The `LocalStandupService` also encrypts PATs before passing them to source providers using `IEncryptionService`:
   ```csharp
   EncryptedPat = _encryptionService.Encrypt(pat)
   ```

3. **No Plain Text:** PATs are never stored in plain text files, logs, or preferences.

4. **Platform Security:**
   - **macOS:** Keychain uses AES-256 encryption
   - **Windows:** DPAPI provides user-specific encryption
   - **iOS/Android:** Hardware-backed keystores where available

## Testing SecureStorage

To verify SecureStorage is working:

1. Run the app and go to Settings
2. Check that pre-populated values appear (Organization, Project, Repository)
3. Modify a value and click Save
4. Restart the app - values should persist

If values don't persist, check:
- Console logs for SecureStorage errors
- Entitlements configuration
- Keychain Access app on macOS for stored entries

## References

- [.NET MAUI SecureStorage Documentation](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage)
- [Apple Keychain Services](https://developer.apple.com/documentation/security/keychain_services)
- [MacCatalyst Entitlements](https://learn.microsoft.com/en-us/dotnet/maui/mac-catalyst/entitlements)
