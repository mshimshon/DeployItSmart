# Deploy scripting utilities
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/mshimshon/deploy_scripting/blob/main/LICENSE)
[![NuGet Version](https://img.shields.io/nuget/v/DeployItSmart)](https://www.nuget.org/packages/DeployItSmart)
[![](https://img.shields.io/nuget/dt/DeployItSmart?label=Downloads)](https://www.nuget.org/packages/DeployItSmart)
[![Build](https://github.com/mshimshon/deploy_scripting/actions/workflows/build.yml/badge.svg)](https://github.com/mshimshon/deploy_scripting/actions/workflows/build.yml)
[![Deploy](https://github.com/mshimshon/deploy_scripting/actions/workflows/deploy.yml/badge.svg)](https://github.com/mshimshon/deploy_scripting/actions/workflows/deploy.yml)


# DeployItSmart

**DeployItSmart** is a lightweight, convention-driven .NET global tool designed to automate versioning, changelog generation, and Git history cleanup for simple single- or multi-project repositories. By parsing standardized commit message tokens (such as `feat:`, `fix:`, or `wip:`), it dynamically calculates the next release version and builds targeted changelogs while keeping deployment workflows fast and uncomplicated.

### Key Features
* **Automated Versioning:** Automatically calculates and increments major, minor, or patch release versions based on structured commit message prefixes.
* **Smart Changelogs:** Generates clean markdown release notes supporting unified root logs or path-segregated multi-project scopes within a single repository.
* **History Sanitization:** Features built-in local utilities to clean up and squash temporary or "dirty" work-in-progress tokens before pushing history.
* **CI/CD Ready:** Optimized for GitHub Actions integration with support for custom configuration templates and workflow-blocking tokens.


## Installation
```
    dotnet tool install -g DeployItSmart
```

## How to Use

- `GetTrackCommitMessagesRunner` to print the commit tracked messages json object array
```bash
depitsmart GetTrackCommitMessagesRunner # All since first commit (Dangerous if massive amount of commit in long lived projects.)
depitsmart GetTrackCommitMessagesRunner -sinceTag "1.0.0" # All since the v1.0.0 tag.
depitsmart GetTrackCommitMessagesRunner -keepDuplicate -sinceTag "1.0.0" # All since the v1.0.0 tag. remove all duplicate token:message from output.
```

- `GetCommitInfoSinceRunner` to print commit infomation in json array since the begining
```bash
depitsmart GetCommitInfoSinceRunner # All since first commit (Dangerous if massive amount of commit in long lived projects.)
depitsmart GetCommitInfoSinceRunner -sinceTag "1.0.0" # All since the v1.0.0 tag.
depitsmart GetCommitInfoSinceRunner -sinceTag "1.0.0" # All since the v1.0.0 tag. remove all duplicate token:message from output.
```

- `GenerateChangeLogRunner` generate changelogs based on commit from the last v
```bash
depitsmart GenerateChangeLogRunner -output "/tmp/changelogs" # Generate log output _.md since the last v tag
depitsmart GenerateChangeLogRunner -output "/tmp/changelogs" -autoFailEmptyResult # Exit 1 if the changelog is empty otherwise it generates empty changelog with message.
depitsmart GenerateChangeLogRunner -output "/tmp/changelogs" -linkCommits # add link on each changes in log to its commit.
```

- `CalculateNextReleaseVersionRunner` print the next version based on commit changes fsince last v tag fails if no changes.
```bash
depitsmart CalculateNextReleaseVersionRunner
```

- `GenerateCustomConfigRunner` creates the user configuration file `.github/config/deploy_it_smart.json` with default parameters.
```bash
depitsmart GenerateCustomConfigRunner
```

- `GetCurrentReleaseVersionRunner` return the current latest v tag version.
```bash
depitsmart GetCurrentReleaseVersionRunner
```

- `LocalTrashCommitCleanerRunner` Clean current commit history from dirty token squashing everything this command is used in our post-commit clean up script.
```bash
# WARNING any working branch pushing dirty commit wip:, trace or trace! will requires a force push because we rewrite history and that history was pushed before clean token appeared
# it will be not possible to push therefore if this occur, do not pull, force push using:
# git fetch origin && git push --force-with-lease origin main
# this will push and destroy dirty remote history it is NOT recommended on multi user branch, DO NOT PERFORM on Master branch it's dangerous and unsecure.
# RECOMMENDED: Never push wip tokens, only push when reaching release token feat: feat!: fix:.... this will cause hisotry to be rewritten locally without future push issues.
depitsmart LocalTrashCommitCleanerRunner
```

## Supported Commit Track Messages
### Release Tokens (Default)
- `feat!:` = Breaking Change. (Cause Next Version Major Increase)
- `feat:` = New Feature Non-Breaking. (Cause Next Version Minor Increase)
- `fix:` = Fix (Non-Breaking). (Cause Next Version Patch Increase)
- `note:` = Release note (Non-Breaking). (Cause Next Version Patch Increase)
- `sponsor:` = Sponsor Note for the release, good to recongnize direct sponsor for the release. (Cause Next Version Patch Increase) (use `@GITHUB_USERNAME` to have changelog generator link the sponsor to gh user)
- `thank:` = Thank you Note. (Cause Next Version Patch Increase) (use `@GITHUB_USERNAME` to have changelog generator link the thank you to gh user)
### Dirty Tokens (Default)
- `wip:` = dirty work in progress should never be push but if it is somehow -> (Cause Next Version Patch Increase)
- `trace:` = will be squashed unless in head commit and will cause workflow trigger without appearing in changelog -> (Cause Next Version Patch Increase)
- `trace!:` = will be squashed unless in head commit and will should NOT trigger workflow if properly configured -> (Cause Next Version Patch Increase)

This should block workflow if head commit reaching branch has trace!... its only useful to push changes into main branch directly such as readme updates or non-release updates.
```yaml
    if: >-
      !contains(github.event.head_commit.message, 'trace!') &&
      !contains(github.event.workflow_run.head_commit.message, 'trace!')
```

## Version System
The version system is quite simple using `1.1.1` pattern and forget about complex `1.0.0-alpha+build` i explored the possibility but failed to see the point.
This tool is design to help maintain packages and projects that are simple and not design for complex build/version systems.
| Token           | Version Change | Example          |
| --------------- | -------------- | ---------------- |
| `feat!:`        | MAJOR          | `1.0.0 -> 2.0.0` |
| `feat!:`        | MINOR          | `1.0.0 -> 1.1.0` |
| `feat!:`        | PATCH          | `1.0.0 -> 1.0.1` |
| `feat!:`        | PATCH          | `1.0.0 -> 1.0.1` |
| `fix:`          | PATCH          | `1.0.0 -> 1.0.1` |
| `note:`         | PATCH          | `1.0.0 -> 1.0.1` |
| `sponsor:`      | PATCH          | `1.0.0 -> 1.0.1` |
| `thank:`        | PATCH          | `1.0.0 -> 1.0.1` |
| `wip:`          | PATCH          | `1.0.0 -> 1.0.1` |
| `trace:`        | PATCH          | `1.0.0 -> 1.0.1` |
| `trace!:`       | PATCH          | `1.0.0 -> 1.0.1` |
| `anything else` | PATCH          | `1.0.0 -> 1.0.1` |

## Multi-Project Setup

The multi-project setup is an advanced feature that allows you to generate segregated changelogs for different sub-projects within a single repository by defining their file paths and friendly names in the configuration file.

### Configure
```json
  "Projects": {
    "_": "My Project Name", // ROOT main log
    "src/DeployItSmart.csproj": "Deploy It Smart" // Specific Sub Project FILE
  },

```
### How It Works

During changelog generation, every commit is analyzed based on the files it modifies:
* **Project-Specific Scoping:** If a modified file falls under a defined project path (e.g., `src/myproject/`), that entire commit's release tokens are assigned to that project.
* **Segregated Output:** Changes targeting `src/myproject/` are written to a dedicated `myproject.md` file. All remaining unassigned changes are routed to a fallback `_.md` file.
* **Unified Main Changelog:** The main repository changelog automatically aggregates these files, displaying structured headers for each defined sub-project.

### Release Strategy & Versioning

To keep system complexity low, **all projects share a single, global version number**. This tool does not support independent versioning per project.

However, you can use conditional deployments (such as GitHub Actions) to skip releases for specific projects if their dedicated changelog is empty:

1. If a commit contains no changes for `myproject`, the `myproject.md` file will not be generated.
2. Your CI/CD workflow can detect this missing file and skip the deployment tag (e.g., `myproject-v$VERSION`) for that specific release.
3. The next time `myproject` receives an update, it will simply jump to the current global version number.

### The Problem This Solves (Our Edge Case)

This feature was built to solve a highly specific architecture bottleneck and may not apply to standard repository setups.

**The exact scenario it was created for:**
We maintain multiple console applications inside a single repository because they all share a core, global library project. We wanted to avoid the overhead of managing 100 separate repositories for 100 individual console apps.

Because our runtime environment dynamically fetches these console applications via API at runtime, we only needed a system that filters for the most recent `[consolename]-v*` tag pattern to pull the latest version. This setup allows the single repository to react only to the specific console apps that actually changed, skipping the rest while maintaining global version alignment.

*Note: If your sub-projects require entirely independent version histories, they should be moved into their own dedicated repositories.*


## Git Post-Commit

It's critical to actively clean up commits as development of a branch goes and the best way to do so is to setup a post commit routine clean up.

You can install the tool using dotnet CLI:

`dotnet tool install -g DeployItSmart`

You chould setup post-commit processing script.

`.git/hooks/post-commit`
```bash
#!/bin/sh
dotnet tool install -g DeployItSmart
depitsmart LocalTrashCommitCleanerRunner
```

To activate it activate the scripts, create `.git/hooks/post-commit` you must run the command:

`git config core.hooksPath .git/hooks`

In Git Bash, make the hook executable with `chmod +x .git/hooks/pre-push`. Git runs it before each push; a nonzero exit from the helper blocks the push, allowing you to address any issues first. Hooks are local to each clone and are not shared automatically, so each contributor must install the hook in their own checkout.



## Workflow Example
```yaml
name: Build

on:
  push:
    branches:
      - main

  workflow_dispatch:


jobs:

  build:
    runs-on: ubuntu-latest
    env:
      GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    permissions:
      id-token: write
      contents: read
    if: >-
      !contains(github.event.head_commit.message, 'trace!') &&
      !contains(github.event.workflow_run.head_commit.message, 'trace!')
    steps:
      - name: Checkout Code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET 10 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Installing Deploy It Smart
        run: |
          dotnet tool install -g DeployItSmart

      - name: Get Last Version Tag
        id: get_last_version
        shell: bash
        run: |
          # 1. Run the command and capture the text output
          OUTPUT=$(depitsmart GetCurrentReleaseVersionRunner) || EXIT_CODE=$?
          if [ "$EXIT_CODE" -ne 0 ]; then
            echo "::error::Script failed with unexpected exit code: $EXIT_CODE"
            exit "$EXIT_CODE"
          fi
          LATEST_TAG=$(echo "$OUTPUT" | tr -d '\r' | tail -n 1 | xargs)
          echo "tag=$LATEST_TAG" >> "$GITHUB_OUTPUT"

      - name: Calculate Version
        id: get_version
        shell: bash
        run: |
          OUTPUT=$(depitsmart CalculateNextReleaseVersionRunner) || EXIT_CODE=$?
          if [ $EXIT_CODE -ne 0 ]; then
            echo "::error::Script failed with unexpected exit code: $EXIT_CODE"
            exit $EXIT_CODE
          fi
          echo "Log: Successfully generated version number -> '$OUTPUT'"
          echo "version=$OUTPUT" >> "$GITHUB_OUTPUT"

      - name: Generate Release Notes
        id: generate_release_notes
        shell: bash
        run: |
          LATEST_TAG="${{ steps.get_last_version.outputs.tag }}"
          VERSION="${{ steps.get_version.outputs.version }}"
          OUTPUT="/tmp/artifacts/output"
          depitsmart GenerateChangeLogRunner -output "$OUTPUT" -linkCommits
          EXIT_CODE=$?
          if [ $EXIT_CODE -ne 0 ]; then
            echo "::error::Script failed with unexpected exit code: $EXIT_CODE"
            exit $EXIT_CODE
          fi

          CHANGELOG_FILE="$OUTPUT/_.md"

          if [ ! -f "$CHANGELOG_FILE" ]; then
            echo "::error::Required file _.md was not found in $OUTPUT"
            exit 1
          elif [ ! -s "$CHANGELOG_FILE" ]; then
            echo "::error::The changelog file _.md exists but it is completely empty!"
            exit 1
          fi

```
