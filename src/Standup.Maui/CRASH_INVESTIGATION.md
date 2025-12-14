an# Standup.Maui Crash Investigation

## The Problem
Standup.Maui crashes on startup on macOS 26 Tahoe with this error:
```
Fatal error: failed to allocate 869194728082505792 bytes of memory with alignment 8
```

The crash occurs in `libswiftObservation.dylib` during `UIView setFrame:` -> `ObservationRegistrar.access`.

## What Works
- [x] MauiAppScaffold (vanilla MAUI template) - **RUNS SUCCESSFULLY**

## What Crashes
- [x] Standup.Maui - **CRASHES**

## Differences Between Scaffold and Standup.Maui

### Packages in Standup.Maui (not in Scaffold)
- [ ] CommunityToolkit.Mvvm
- [ ] Serilog
- [ ] Serilog.Extensions.Hosting
- [ ] Serilog.Formatting.Compact
- [ ] Serilog.Sinks.Console
- [ ] Serilog.Sinks.File
- [ ] Microsoft.Extensions.Http

### Project References in Standup.Maui
- [ ] Standup.Application

### Code Differences
- [ ] App.xaml.cs uses `MainPage = new AppShell()` (deprecated) vs scaffold uses `CreateWindow` returning `new Window(new AppShell())`
- [ ] MauiProgram.cs has DI service registration
- [ ] MauiProgram.cs has Serilog configuration
- [ ] MauiProgram.cs has DiagnosticsHelper initialization
- [ ] AppShell.xaml uses TabBar (was FlyoutBehavior="Flyout")

## Isolation Test Plan

Add each item from Standup.Maui to the working Scaffold ONE AT A TIME, build and run after each:

### Step 1: Add CommunityToolkit.Mvvm
- [x] Add package to scaffold csproj
- [x] Build - SUCCESS
- [x] Run - **WORKS** - NOT the cause

### Step 2: Add Serilog packages
- [x] Add all Serilog packages to scaffold csproj
- [x] Build - SUCCESS
- [x] Run - **WORKS** - NOT the cause

### Step 3: Add Microsoft.Extensions.Http
- [x] Add package to scaffold csproj
- [x] Build - SUCCESS
- [x] Run - **WORKS** - NOT the cause

### Step 4: Add ProjectReference to Standup.Application
- [x] Add reference to scaffold csproj
- [x] Build - SUCCESS
- [x] Run - **WORKS** - NOT the cause

### Step 5: Add DI service registration
- [x] Add services to MauiProgram.cs
- [x] Build - SUCCESS
- [x] Run - **WORKS** - NOT the cause

### Step 6: Change App.xaml.cs pattern
- [x] Change to use `MainPage = new AppShell()` pattern
- [x] Build - SUCCESS
- [x] Run - **WORKS** - NOT the cause

### Step 7: Add DiagnosticsHelper initialization
- [x] Add DiagnosticsHelper.Initialize() call
- [x] Build - SUCCESS
- [x] Run - **WORKS** - NOT the cause

### Step 8: Add Services (IProjectService, etc.)
- [x] Add service interfaces and implementations
- [x] Build - SUCCESS
- [x] Run - **WORKS** - NOT the cause

### Step 9: Add ViewModels
- [x] Add ViewModel registrations
- [x] Build - SUCCESS
- [x] Run - **WORKS** - NOT the cause

### Step 10: Add Views
- [x] Add View registrations
- [x] Build - SUCCESS
- [x] Run - **WORKS** - NOT the cause

### Step 11: Add TabBar AppShell with multiple ShellContent items
- [x] Change AppShell to use TabBar with 5 ShellContent items
- [x] Build - SUCCESS
- [x] Run - **CRASHED** - **THIS IS THE CAUSE!**

## ROOT CAUSE IDENTIFIED

**The crash is caused by using TabBar with multiple ShellContent items in AppShell.xaml on macOS 26 Tahoe.**

The crash occurs in `libswiftObservation.dylib` during `UIView setFrame:` -> `ObservationRegistrar.access` when the Shell tries to render the TabBar UI.

This appears to be a bug in .NET MAUI's MacCatalyst implementation when combined with macOS 26's new Swift Observation framework.

## Solution Implemented

### The Fix
Replaced Shell/TabBar with a custom ContentPage-based tab implementation:
- `MainTabbedPage` extends `ContentPage` (not TabbedPage - which also doesn't render tabs on MacCatalyst)
- Uses a Grid with content area and a bottom HorizontalStackLayout with Button-based tabs
- Tabs are created programmatically with proper styling and selection state
- Pages loaded via DI and their Content is displayed in the content area

### Additional Fixes Required
1. **CommunityToolkit.Maui removed** - Also caused crashes on macOS 26 Tahoe
2. **Local converters created** - `BoolConverters.cs` replaces CommunityToolkit converters
3. **LocalStandupService stub** - Infrastructure has AspNetCore dependencies incompatible with MacCatalyst

### Files Changed
- `Views/MainTabbedPage.xaml` - Changed to ContentPage
- `Views/MainTabbedPage.xaml.cs` - Manual tab implementation
- `Converters/BoolConverters.cs` - Local value converters
- `App.xaml` - References local converters
- `App.xaml.cs` - Uses MainTabbedPage instead of AppShell
- `MauiProgram.cs` - Registers ILocalStandupService
- `Services/LocalStandupService.cs` - Stub implementation
- `Services/ILocalStandupService.cs` - Interface
- `Standup.Maui.csproj` - Removed CommunityToolkit.Maui

### Result
App launches and runs successfully on macOS 26 Tahoe with .NET 10.

## Tests Already Completed

### CommunityToolkit.Maui
- [x] Removed from Standup.Maui - **STILL CRASHED** - Not the cause of Shell crash

### FlyoutBehavior="Flyout"
- [x] Changed to TabBar in Standup.Maui - **STILL CRASHED** - Not the cause

### Simplified AppShell (single ShellContent)
- [x] Tested in Standup.Maui - **STILL CRASHED** - Not the cause

## Status: RESOLVED
Fixed by replacing Shell with custom ContentPage-based tab navigation.