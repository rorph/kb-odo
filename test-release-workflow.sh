#!/bin/bash

# Test script to validate GitHub Actions workflow locally using act
# Requires: https://github.com/nektos/act

echo "Testing GitHub Actions Release Workflow Locally"
echo "============================================="

# Check if act is installed
if ! command -v act &> /dev/null; then
    echo "❌ 'act' is not installed. Please install it first:"
    echo "   - macOS: brew install act"
    echo "   - Linux: curl https://raw.githubusercontent.com/nektos/act/master/install.sh | sudo bash"
    echo "   - Or see: https://github.com/nektos/act#installation"
    exit 1
fi

# Create a temporary .secrets file for testing
SECRETS_FILE=".secrets"
echo "Creating temporary secrets file..."
cat > "$SECRETS_FILE" << EOF
GITHUB_TOKEN=fake-token-for-testing
EOF

# Test the release workflow with manual dispatch
echo ""
echo "Testing manual workflow dispatch with version 1.0.0..."
act workflow_dispatch \
    -W .github/workflows/release.yml \
    -s GITHUB_TOKEN=fake-token-for-testing \
    --input version=1.0.0 \
    --dryrun \
    --platform windows-latest=catthehacker/ubuntu:act-latest

# Clean up
rm -f "$SECRETS_FILE"

echo ""
echo "✅ Workflow syntax validation complete!"
echo ""
echo "To test with a real GitHub token:"
echo "1. Create a Personal Access Token with 'repo' scope"
echo "2. Run: act workflow_dispatch -W .github/workflows/release.yml -s GITHUB_TOKEN=your-token --input version=1.0.0"