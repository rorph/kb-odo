# GitHub Actions Release Workflow Troubleshooting

## Issue: 403 Forbidden Error When Creating Release

### Root Causes and Solutions

#### 1. **Missing Workflow Permissions** ✅ FIXED
The workflow needs explicit permissions to create releases.

**Solution Applied:**
```yaml
permissions:
  contents: write
  discussions: write
```

#### 2. **Incorrect Token Usage** ✅ FIXED
The `softprops/action-gh-release` action expects the token in the `with` section, not `env`.

**Before (Incorrect):**
```yaml
env:
  GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**After (Correct):**
```yaml
with:
  token: ${{ secrets.GITHUB_TOKEN }}
```

#### 3. **Repository Settings Issues**
Check these repository settings:

1. **Actions Permissions:**
   - Go to Settings → Actions → General
   - Under "Workflow permissions", select:
     - ✅ "Read and write permissions"
     - ✅ "Allow GitHub Actions to create and approve pull requests"

2. **Protected Tags:**
   - Go to Settings → Tags → Protected tags
   - Ensure `v*` tags are NOT protected, or add an exception for GitHub Actions

3. **Branch Protection Rules:**
   - If main/master branch is protected, ensure "Allow force pushes" includes GitHub Actions

### Testing the Fix

#### Method 1: Manual Workflow Dispatch
```bash
# From the GitHub Actions tab, manually trigger the workflow
# Select "Release" workflow → "Run workflow" → Enter version "1.0.0"
```

#### Method 2: Create a Test Tag
```bash
# Create and push a test tag
git tag v1.0.0-test
git push origin v1.0.0-test

# Watch the Actions tab for the workflow run

# Clean up after testing
git push --delete origin v1.0.0-test
git tag -d v1.0.0-test
```

#### Method 3: Local Testing with act
```bash
# Install act (GitHub Actions local runner)
# macOS: brew install act
# Linux: curl https://raw.githubusercontent.com/nektos/act/master/install.sh | sudo bash

# Test the workflow
act workflow_dispatch -W .github/workflows/release.yml \
    -s GITHUB_TOKEN=$GITHUB_TOKEN \
    --input version=1.0.0
```

### Additional Checks

#### 1. Verify Token Permissions
If using a Personal Access Token instead of GITHUB_TOKEN:
- Required scopes: `repo` (full control)
- For public repos only: `public_repo`

#### 2. Check Rate Limits
```bash
curl -H "Authorization: token YOUR_TOKEN" \
     https://api.github.com/rate_limit
```

#### 3. Validate Workflow Syntax
```bash
# Use GitHub's workflow linter
curl -X POST \
  -H "Authorization: token YOUR_TOKEN" \
  -H "Accept: application/vnd.github.v3+json" \
  https://api.github.com/repos/OWNER/REPO/actions/workflows/release.yml/dispatches \
  -d '{"ref":"main","inputs":{"version":"1.0.0"}}'
```

### Common Error Messages and Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| `403: Resource not accessible by integration` | Missing permissions | Add `permissions: contents: write` |
| `403: Must have admin rights to Repository` | Token lacks permissions | Use token with `repo` scope |
| `422: Validation Failed` | Tag already exists | Delete existing tag or use different version |
| `404: Not Found` | Wrong repository or workflow path | Verify repository name and workflow file |

### Final Checklist

- [ ] Workflow has `permissions: contents: write`
- [ ] Token is passed via `with: token:` not `env:`
- [ ] Repository allows Actions to write
- [ ] No branch/tag protection blocking releases
- [ ] Using `softprops/action-gh-release@v1` (latest)
- [ ] Tag format matches trigger pattern (`v*`)

### If Still Failing

1. **Enable Debug Logging:**
   ```yaml
   env:
     ACTIONS_STEP_DEBUG: true
     ACTIONS_RUNNER_DEBUG: true
   ```

2. **Check Action Logs:**
   - Click on the failed workflow run
   - Expand the "Create Release" step
   - Look for specific error details

3. **Try Alternative Release Action:**
   ```yaml
   - name: Create Release
     uses: ncipollo/release-action@v1
     with:
       token: ${{ secrets.GITHUB_TOKEN }}
       tag: v${{ steps.get_version.outputs.version }}
       name: Release v${{ steps.get_version.outputs.version }}
       draft: false
       prerelease: false
   ```

The fixes applied should resolve the 403 error. The key changes were adding explicit permissions and correctly passing the token to the action.