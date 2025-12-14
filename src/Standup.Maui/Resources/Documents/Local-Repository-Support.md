# Local Repository Support

The Standup app supports adding local git repositories directly, without requiring a Personal Access Token (PAT) for basic commit tracking.

## How It Works

Repositories can be:
- **Local only**: No remote configured, purely local git repository
- **Local + Remote**: Has a remote (GitHub/Azure DevOps) - can read commits locally
- **Remote only**: No local copy, requires PAT for all operations

### When a Local Path is Set

1. **Commits**: Read directly from `git log` - no PAT needed
2. **PRs & Work Items**: Optionally fetched via API if PAT is provided
3. **Offline Support**: Commit data works without network access

### Benefits

| Feature | Local Repo | Remote Only |
|---------|------------|-------------|
| View commits | No PAT needed | Requires PAT |
| View PRs | Optional PAT | Requires PAT |
| View work items | Optional PAT | Requires PAT |
| Works offline | Yes (commits) | No |
| Speed | Faster | API latency |

## Adding a Local Repository

### Method 1: Browse for Folder

1. Go to **Groups** tab
2. Click **+ Repo** on your group
3. Click **Browse Local Repository...**
4. Select any file inside your git repository
5. The app automatically detects:
   - Repository root (walks up to find `.git` folder)
   - Remote URL (GitHub or Azure DevOps)
   - Organization, project, and repository names
6. Fill in the **Client Code**
7. (Optional) Add PAT for PR/work item access
8. Click **Add Repository**

### Method 2: Manual Entry

1. Click **+ Repo**
2. Fill in all fields manually:
   - Source Type (GitHub or Azure DevOps)
   - Organization
   - Project (Azure DevOps only)
   - Repository name
3. No PAT needed for local-only commit tracking

## Supported Remote URL Formats

The app auto-detects repository info from these URL patterns:

### GitHub
```
https://github.com/org/repo.git
git@github.com:org/repo.git
```

### Azure DevOps
```
https://dev.azure.com/org/project/_git/repo
git@ssh.dev.azure.com:v3/org/project/repo
https://org.visualstudio.com/project/_git/repo
```

## PAT Requirements

| Operation | PAT Required? |
|-----------|---------------|
| List commits (local repo) | No |
| List commits (remote only) | Yes |
| View PRs | Yes |
| View work items | Yes |
| Validate connection | Yes |

## Technical Details

### Git Config Parsing

The app reads `.git/config` to extract remote information:

```ini
[remote "origin"]
    url = git@github.com:your-org/your-repo.git
```

This is parsed to extract:
- `SourceType`: GitHub
- `Organization`: your-org
- `Repository`: your-repo

### Local Git Commands

Commits are read using:
```bash
git log --format="%H%x00%h%x00%an%x00%ae%x00%aI%x00%s%x00%b%x00%x01" \
    -n 100 \
    --author="filter" \
    --since="2025-12-06" \
    --until="2025-12-13"
```

## Troubleshooting

### "Not a git repository"

- Ensure the folder contains a `.git` directory
- Try selecting a file deeper in the repository

### "Could not parse remote URL"

- The repository might have a non-standard remote format
- Enter details manually instead

### Commits not showing

- Check the **Author Filter** matches your git user.name or user.email
- Verify the date range includes your commits
- Run `git log --author="your-filter"` locally to test

### PRs/Work Items missing

- PRs and work items require a PAT
- Add your PAT in the repository configuration
- Ensure the PAT has appropriate scopes (repo, work_items)
