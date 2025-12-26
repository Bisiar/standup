# Troubleshooting

Common issues and solutions for the Standup Automation Platform.

## API Issues

### "User not authenticated"

**Cause**: Missing or invalid authentication token.

**Solutions**:
1. Verify the Authorization header is present
2. Check token hasn't expired
3. Ensure token has correct audience (`api://your-client-id`)
4. Verify user has been provisioned in the tenant

### "Failed to connect to repository"

**Cause**: Invalid credentials or repository configuration.

**Solutions**:
1. Verify PAT hasn't expired
2. Check PAT has required permissions
3. Ensure repository path is correct
4. For Azure DevOps, verify project name is included

### "AI summarization failed"

**Cause**: AI Foundry connection or quota issues.

**Solutions**:
1. Check AI Foundry endpoint is correct
2. Verify deployment name exists
3. Check quota limits in Azure portal
4. Review Application Insights for detailed errors

## MAUI App Issues

### App crashes on startup

**Solutions**:
1. Clear app data and cache
2. Reinstall the application
3. Check device meets minimum requirements
4. Review crash logs in device settings

### "No projects configured"

**Solutions**:
1. Add a project in the Projects page
2. Verify API endpoint is accessible
3. Check network connectivity

### Repository sync fails

**Solutions**:
1. Verify PAT is valid and not expired
2. Check author identifier matches your account
3. Ensure repository exists and is accessible
4. Review error message for specific details

## Teams Integration Issues

### Bot doesn't respond

**Cause**: Bot registration or permission issues.

**Solutions**:
1. Verify bot is registered in Azure Bot Service
2. Check messaging endpoint is correct
3. Ensure Teams app manifest has correct bot ID
4. Review bot service logs for errors

### Messages not posting to channel

**Solutions**:
1. Verify `ChannelMessage.Send` permission is consented
2. Check subscription is active for the channel
3. Ensure bot is added to the team
4. Verify channel ID is correct

### Direct messages not received

**Solutions**:
1. Check `Chat.ReadWrite` permission
2. Verify Teams user ID is configured
3. Ensure 1:1 chat can be created with the user

## Authentication Issues

### "Invalid token"

**Solutions**:
1. Clear cached tokens and re-authenticate
2. Verify client ID matches registered app
3. Check tenant ID is correct
4. Ensure scopes are properly configured

### "Consent required"

**Solutions**:
1. Have admin grant consent in Azure portal
2. Or use incremental consent flow
3. Verify all required permissions are in manifest

### CORS errors

**Solutions**:
1. Add origin to allowed origins in API
2. Verify using correct API endpoint
3. Check for proxy/firewall interference

## Data Issues

### Empty standup reports

**Cause**: No commits found in date range.

**Solutions**:
1. Verify author identifier matches commit author
2. Check date range (default: last working day)
3. Ensure repositories are active
4. Verify commits exist in configured branch

### Missing work items

**Cause**: Azure DevOps query issues.

**Solutions**:
1. Verify author identifier matches assignee
2. Check work item states (Active, In Progress)
3. Ensure PAT has work item read permission
4. Verify project name is correct

### Duplicate entries

**Solutions**:
1. Check for duplicate repository configurations
2. Verify author identifier isn't matching multiple users
3. Review aggregation logic in logs

## Performance Issues

### Slow standup generation

**Cause**: Multiple API calls to source providers.

**Solutions**:
1. Reduce number of configured repositories
2. Narrow date range
3. Check source provider API rate limits
4. Review API logs for slow operations

### High Azure costs

**Solutions**:
1. Review Cosmos DB throughput settings
2. Check AI Foundry token usage
3. Enable scale-to-zero for Functions
4. Review Application Insights retention

## Logging

### Enable detailed logging

Set in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Standup": "Debug"
    }
  }
}
```

### View logs

```bash
# Azure Functions logs
az webapp log tail --name your-function --resource-group your-rg

# Application Insights
az monitor app-insights query \
  --app your-app-insights \
  --analytics-query "traces | where timestamp > ago(1h)"
```

## Getting Help

1. Check existing issues in the repository
2. Review Application Insights for detailed errors
3. Enable debug logging for more information
4. Contact the development team with:
   - Error messages
   - Steps to reproduce
   - Environment details
   - Relevant logs
