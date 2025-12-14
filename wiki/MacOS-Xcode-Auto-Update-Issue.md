# macOS Xcode Auto-Update Breaking Development Environment

## Incident: December 13, 2025

### What Happened

During active development of the Standup.Maui application, the build suddenly started failing with the error:

```
xcrun: error: sh -c '/Applications/Xcode-26.1.app/Contents/Developer/usr/bin/xcodebuild -sdk macosx -find mdimport 2> /dev/null' failed with exit code 17920: (null) (errno=No such file or directory)
xcrun: error: unable to find utility "mdimport", not a developer tool or in PATH
```

This was suspicious because the build had been working just minutes before.

### Root Cause

The **App Store's automatic update feature** (`appstoreagent`) silently downloaded and installed Xcode 26.2 beta at 11:23 AM while development was in progress.

Evidence from `/var/log/install.log`:
```
2025-12-13 11:23:45-05 JamessMacStudio-1 installd[62395]: Installed "Xcode" (26.2)
2025-12-13 11:23:45-05 JamessMacStudio-1 installd[62395]: PackageKit: Touched bundle /Applications/Xcode-26.1.app
```

The new Xcode was installed as `/Applications/Xcode-26.1.app` to avoid overwriting the existing `/Applications/Xcode.app`.

### Why It Broke the Build

1. The .NET SDK detected multiple Xcode installations
2. The SDK's configuration or cache referenced the new `Xcode-26.1.app` path
3. The new Xcode installation was either incomplete or had a different configuration
4. Build tools like `mdimport` were not found at the expected paths

### How to Diagnose

1. **Check for new Xcode installations:**
   ```bash
   ls -la /Applications/ | grep -i xcode
   ```

2. **Check current xcode-select path:**
   ```bash
   xcode-select -p
   ```

3. **Check install logs for recent Xcode installs:**
   ```bash
   tail -100 /var/log/install.log | grep -i xcode
   ```

4. **Check Software Update logs:**
   ```bash
   /usr/bin/log show --last 2h --predicate 'subsystem == "com.apple.SoftwareUpdate"' | head -50
   ```

### Resolution

Switch back to the working Xcode installation:

```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
```

Then rebuild:
```bash
dotnet clean
dotnet build
```

### Prevention

To prevent App Store from automatically installing updates:

1. **System Settings > App Store**
2. Disable **"Install application updates"** (or on older macOS: uncheck "Automatically keep my Mac up to date")

Alternatively, you can disable automatic updates entirely:
```bash
sudo defaults write /Library/Preferences/com.apple.SoftwareUpdate AutomaticDownload -bool false
```

### Impact

- Build failures during active development
- Time spent diagnosing unexpected error
- Potential confusion when error messages reference non-existent or unexpected paths

### Lessons Learned

1. **Automatic updates can break development environments** - especially for tools like Xcode that have deep system integration
2. **Monitor for new applications appearing** - unexpected files in `/Applications` owned by root are a sign of automatic updates
3. **Keep xcode-select in mind** - when Xcode-related builds fail unexpectedly, check which Xcode is selected
4. **Log files are essential** - `/var/log/install.log` quickly identified the culprit

### Related Files

- `/var/log/install.log` - System installation log
- `/Library/Preferences/com.apple.SoftwareUpdate.plist` - Software Update preferences
- Output of `xcode-select -p` - Current Xcode developer directory
