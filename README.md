# Deploy scripting utilities
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/mshimshon/DeployItSmart/blob/main/LICENSE)
[![NuGet Version](https://img.shields.io/nuget/v/DeployItSmart)](https://www.nuget.org/packages/DeployItSmart)
[![](https://img.shields.io/nuget/dt/DeployItSmart?label=Downloads)](https://www.nuget.org/packages/DeployItSmart)
[![Preview](https://github.com/mshimshon/DeployItSmart/actions/workflows/build_preview.yml/badge.svg)](https://github.com/mshimshon/DeployItSmart/actions/workflows/build_preview.yml)
[![Stable](https://github.com/mshimshon/DeployItSmart/actions/workflows/build_release.yml/badge.svg)](https://github.com/mshimshon/DeployItSmart/actions/workflows/build_release.yml)
[![Deploy](https://github.com/mshimshon/DeployItSmart/actions/workflows/deploy.yml/badge.svg)](https://github.com/mshimshon/DeployItSmart/actions/workflows/deploy.yml)

# NOT PRODUCTION READY!
Version are all messed up unfortunately. do not use for production.

## DISCLAIMER OF HERATIC AND IDIOTIC BEHAVIOR
> Because im a rebel testing on live repos i'll let you know when its good to go what ever version it will be xD
>
> *Do not try, at home! try at work instead :O, nooo he didn't!*

***
# Language
Built in `.NET 10` but completely code agnostic, work with anything and `git`.

# DeployItSmart

**DeployItSmart** is a lightweight, convention-driven tool designed to automate versioning, changelog generation, and Git history cleanup for simple single- or multi-project repositories. By parsing standardized commit message tokens (such as `feat:`, `fix:`, or `wip:`), it dynamically calculates the next release version and builds targeted changelogs while keeping deployment workflows fast and uncomplicated.

### Key Features
* **Social Hooks:** Ability to push `sponsor:` `thank:` directly to be reflected in the final changelog to drive community engagement.
* **Advance Parser:** Ability to use regex to parse commit metadata to changelog markdown for example `sponsor~: King Pin driving rock solid support THANK! you rock {gh:mshimshon:Maksim Shimshon}` to translate to markdown link of the github profile.
* **Automated Versioning:** Automatically calculates and increments major, minor, or patch release versions based on structured commit message prefixes.
* **Smart Changelogs:** Generates clean markdown release notes supporting unified root logs or path-segregated multi-project scopes within a single repository.
* **History Sanitization:** Features built-in local utilities to clean up and squash temporary or "dirty" work-in-progress tokens before pushing history.
* **CI/CD Ready:** Optimized for GitHub Actions integration with support for custom configuration templates and workflow-blocking tokens.


## Installation
```
dotnet tool install -g DeployItSmart

```

## How to Use
- `--verbose` to print details (Not Recommended in Production, Less Runtime Runner)
- `GetTrackCommitMessagesRunner` to print the commit tracked messages json object array
```bash
depitsmart GetTrackCommitMessagesRunner # All since first commit (Dangerous if massive amount of commit in long lived projects.)
depitsmart GetTrackCommitMessagesRunner --sinceTag "1.0.0" # All since the v1.0.0 tag.
depitsmart GetTrackCommitMessagesRunner --keepDuplicate --sinceTag "1.0.0" # All since the v1.0.0 tag. will keep all duplicate token:message from output.
```

- `GetCommitInfoSinceRunner` to print commit infomation in json array since the begining
```bash
depitsmart GetCommitInfoSinceRunner # All since first commit (Dangerous if massive amount of commit in long lived projects.)
depitsmart GetCommitInfoSinceRunner --sinceTag "1.0.0" # All since the v1.0.0 tag.
depitsmart GetCommitInfoSinceRunner --sinceTag "1.0.0" # All since the v1.0.0 tag. remove all duplicate token:message from output.
```

- `GenerateChangeLogRunner` generate changelogs based on commit from the last v
```bash
depitsmart GenerateChangeLogRunner --output "/tmp/changelogs" # Generate log output _.x.x.x.md since the last v tag
depitsmart GenerateChangeLogRunner --output "/tmp/changelogs" --sinceTag "1.0.0" # Generate log output _.x.x.x.md since the version will still generate for the next version but will include the changes since
depitsmart GenerateChangeLogRunner --output "/tmp/changelogs" --sinceTag "1.0.0" --allowUnsafeVersionFromSinceTag # same as before but will generate file version from 'sinceTag' rather than current it is dangerously unsafe.
depitsmart GenerateChangeLogRunner --output "/tmp/changelogs" --autoFailEmptyResult # Exit 1 if the changelog is empty otherwise it generates empty changelog with message.
depitsmart GenerateChangeLogRunner --output "/tmp/changelogs" --linkCommits # add link on each changes in log to its commit.
depitsmart GenerateChangeLogRunner --output "/tmp/changelogs"  --previewMode # Same be will generate
```



- `GenerateCustomConfigRunner` creates the user configuration file `.github/config/deploy_it_smart.json` with default parameters.
```bash
depitsmart GenerateCustomConfigRunner # can be safely ran if you have config already it will update any missing aaaaaproperty with default but will not override your changes.
```

- `GetCurrentPreReleaseVersionRunner` return the current latest v1.0.0-preview.x tag
```bash
depitsmart GetCurrentPreReleaseVersionRunner
depitsmart GetCurrentPreReleaseVersionRunner --ghOutput # Print the value into github output 'version_current_preview' accessible ${{ steps.STEPID.outputs.version_current_preview }} (crash if value empty for safety)
```

- `GetCurrentReleaseVersionRunner` return the current latest v tag version.
```bash
depitsmart GetCurrentReleaseVersionRunner
depitsmart GetCurrentReleaseVersionRunner --ghOutput # Print the value into github output 'version_current' accessible ${{ steps.STEPID.outputs.version_current }} (crash if value empty for safety)
```
- `CalculateNextReleaseVersionRunner` print the next version based on commit changes fsince last v tag fails if no changes.
```bash
depitsmart CalculateNextReleaseVersionRunner # calculate next natural version since last v tag
depitsmart CalculateNextReleaseVersionRunner  --sinceTag "1.0.0" --allowUnsafeVersionFromSinceTag # Will calculate the version since the specified tag dangerous should not be use on production.
depitsmart CalculateNextReleaseVersionRunner  --previewMode # will calculate the upcoming expected release as preview.X + N
depitsmart CalculateNextReleaseVersionRunner  --previewMode --ghOutput # Print the value into github output 'version_next_preview' accessible ${{ steps.STEPID.outputs.version_next_preview }} (crash if value empty for safety)
depitsmart CalculateNextReleaseVersionRunner --ghOutput # Print the value into github output 'version_next' accessible ${{ steps.STEPID.outputs.version_next }} (crash if value empty for safety)
```
- `LocalTrashCommitCleanerRunner` Clean current commit history from dirty token squashing everything this command is used in our post-commit clean up script.
```bash
# WARNING any working branch pushing dirty commit wip will requires a force push because we rewrite history and that history was pushed before clean token appeared
# it will be not possible to push therefore if this occur, do not pull, force push using:
# git fetch origin && git push --force-with-lease origin main
# this will push and destroy dirty remote history it is NOT recommended on multi user branch, DO NOT PERFORM on Master branch it's dangerous and unsecure.

depitsmart LocalTrashCommitCleanerRunner
```

## Social Driven Development

Okay people! Time to throw out *Domain Driven Development* and start working with **Social Driven Development**!

The tool is built around this exact idea: making commits meaningful not only to us, but to the community supporting our hard-worked projects. It puts credit back where credit is due effortlessly. And yes—I mean *yes*—I am that lazy, and so are you!

### The Core Benefits
* **Better Commit Messages:** It forces contributors to carefully document their commits as they go.
* **Community Recognition:** Paired with the `sponsor:` and `thank:` tokens, it enables full, automatic recognition for the people opening issues, contributing code, or sponsoring releases.

### How the Advanced Parser Works
The system uses post-processing to handle complex regex group captures. It transforms raw metadata into clean Markdown links using the group extractor `{gp:0}` by default.

* **The Purpose:** Add direct links without polluting your git commit messages with long URLs—only small metadata gets parsed.
* **Security First:** For security reasons, all advanced parsing matchers are **disabled by default**. You can activate them in your repository configuration easily.

## Script Based Runners
The tool has experiemental script base runner disabled by default.

That's was for me the oportunity to wet myself with `.csx` file c# base scripts runtime compiled and executed allowing you to built your own light weight command runner using c# directly in the repos.



## Supported Commit Track Messages
| Token       | Description                                                                     |
| ----------- | ------------------------------------------------------------------------------- |
| `feat!:`    | New Feature Breaking Change                                                     |
| `feat~:`    | Important Feature Without Breaking                                              |
| `feat:`     | Non Breaking Feature                                                            |
| `fix!:`     | Fix With Breaking Changes                                                       |
| `fix~:`     | Important Fix non-breaking                                                      |
| `fix:`      | Non Breaking Fix                                                                |
| `note!:`    | Release Note with Breaking Changes                                              |
| `note~:`    | Important Note without Breaking Changes                                         |
| `note:`     | Release Note                                                                    |
| `sponsor~:` | Realese Super Duper Sponsor!                                                    |
| `sponsor:`  | Release Sponsor to link username                                                |
| `thank~:`   | Release Super Thanks                                                            |
| `thank:`    | Release Thank to link username                                                  |
| `wip:`      | Dirty Work in progress, should not be pushed. cleaned up by post-commit command |
| `skipci~:`  | Push/Merge without triggering the release workflow if implemented               |

### Blocking Workflows with `skipci~`

The `skipci~` token blocks your CI/CD workflow if the head commit hitting the branch contains it. This is incredibly useful for pushing changes directly to your main branch that shouldn't trigger a full release cycle, such as README updates or non-release documentation tweaks.

```yaml
    if: >-
      !contains(github.event.head_commit.message, 'skipci~') &&
      !contains(github.event.workflow_run.head_commit.message, 'skipci~')
```

## Version System

Exception for Preview Strategy: at least +1 minor next release cycle, only hotfixes are patch.

This system uses a streamlined `1.1.1` versioning pattern, keeping complex syntax (like `1.0.0-alpha.1`) restricted to dedicated pre-release branches. It offers a lightweight, boilerplate-free alternative to heavy enterprise solutions for managing packages and projects.


| Token       | Description                                                       | Version Change | Example          |
| ----------- | ----------------------------------------------------------------- | -------------- | ---------------- |
| `feat!:`    | New Feature Breaking Change                                       | MAJOR          | `1.0.0 -> 2.0.0` |
| `feat~:`    | Important Feature Without Breaking                                | MINOR          | `1.0.0 -> 1.1.0` |
| `feat:`     | Non Breaking Feature                                              | MINOR          | `1.0.0 -> 1.1.0` |
| `fix!:`     | Fix With Breaking Changes                                         | MAJOR          | `1.0.0 -> 2.0.0` |
| `fix~:`     | Important Fix non-breaking                                        | PATCH          | `1.0.0 -> 1.0.1` |
| `fix:`      | Non Breaking Fix                                                  | PATCH          | `1.0.0 -> 1.0.1` |
| `note!:`    | Release Note with Breaking Changes                                | MAJOR          | `1.0.0 -> 2.0.0` |
| `note~:`    | Important Note without Breaking Changes                           | PATCH          | `1.0.0 -> 1.0.1` |
| `note:`     | Release Note                                                      | PATCH          | `1.0.0 -> 1.0.1` |
| `sponsor~:` | Realese Super Duper Sponsor!                                      | PATCH          | `1.0.0 -> 1.0.1` |
| `sponsor:`  | Release Sponsor                                                   | PATCH          | `1.0.0 -> 1.0.1` |
| `thank~:`   | Release Super Thanks                                              | PATCH          | `1.0.0 -> 1.0.1` |
| `thank:`    | Release Thank                                                     | PATCH          | `1.0.0 -> 1.0.1` |
| `wip:`      | Dirty Work in progress, should not be pushed.                     | PATCH          | `1.0.0 -> 1.0.1` |
| `skipci~:`  | Push/Merge without triggering the release workflow if implemented | PATCH          | `1.0.0 -> 1.0.1` |

### Rollover Versioning
The system enforces a default limit of `10000` increments per tier to trigger automatic version rollovers.

* **Mechanism:** `10000 Patches` -> `+1 Minor`, and `10000 Minors` -> `+1 Major`.
* **Cap:** This logic purely calculates the next release version; multiple rollover of the same will still only increment the target version by a single unit.

*Note: While automated workflow pushes automatically increment the patch version by default, these simulated updates do not contribute toward the rollover cap. The `10000` threshold must be met exclusively by valid, explicit commit tokens.*


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

During changelog generation, every single commit is analyzed based on the files it modifies:

* **Project-Specific Scoping:** If a modified file falls under a defined project path (e.g., `src/myproject/`), the release tokens for that entire commit are automatically assigned to that specific project.
* **Segregated Output:** Changes targeting `src/myproject/` are written directly to a dedicated `myproject.md` file, while all remaining unassigned changes are routed to a fallback `_.md` file.
* **Unified Main Changelog:** The main repository changelog automatically aggregates these individual files, displaying structured headers for each defined sub-project.

However, you can use conditional deployments (such as GitHub Actions) to skip releases for specific projects if no commits changes its child files (folder based):

- **Missing Changelog**: If a commit contains no changes for `myproject`, the `myproject.1.0.1.md` file will not be generated.
- **Skipped Deployment**: Your CI/CD workflow can detect this missing file and skip the deployment tag (e.g., `myproject-v$VERSION`) for that specific release.
- **Version Bumping**: The next time `myproject` receives an update, it will simply jump to the next global version number.

> **Note**: If your sub-projects require entirely independent version histories, they should be moved into their own dedicated repositories.


## How Preview Versioning Works

Preview releases are entirely optional. Teams that do not require preview, they can release stable versions directly through their standard promotion process PR to main branch which at very least increase patch.

The preview versioning model uses a **promotion branch strategy** combined short lived feature branch to merge in `main-dev` (default).

### Branch Structure

- **Main (stable) branch** contains production-ready releases only.
- **Development branch** is the integration branch for new features, fixes, and future releases.
- All significant changes are first developed in the development branch and later promoted to the main branch through pull requests.

### Preview Version Calculation

A key design principle is that the **development preview version is always at least one minor version ahead of the current stable release**.

Examples:

```text
Stable: v1.0.0
Preview: v1.1.0-preview.1
Preview: v1.1.0-preview.2
```

```text
Stable: v2.3.0
Preview: v2.4.0-preview.1
```

This separation assumes repository policies are correctly enforced and ensures that ongoing development never conflicts with maintenance work performed on the current stable release line.

### Hotfix Support

Because the development branch always targets a future version, hotfixes can continue to be applied directly to the stable branch without negatively affecting preview development.

Example:

```text
Stable Release Line
v1.0.0
 ├─ v1.0.1
 ├─ v1.0.2
 ├─ ...
 └─ v1.0.99

Development Line
v1.1.0-preview.1
 ├─ v1.1.0-preview.2
 └─ ...
```

If a hotfix is required:

1. Apply the fix to the stable branch.
2. Release the hotfix version.
3. Create a back merge the same change into the development branch.

Example:

```text
v1.0.98
 └─ Hotfix
     └─ v1.0.99

Development:
v1.1.0-preview.5
 └─ Re-PR same hotfix
     └─ v1.1.0-preview.6
```

This guarantees that supported stable releases can continue receiving patches while future development progresses independently.

### Major Version Rollovers

Breaking changes and major architectural modifications should never be introduced directly into the stable branch.

Instead, they begin in the development branch as a new preview series.

Example:

```text
v1.0.0
└── v1.1.0-preview.1
    └── v1.1.0-preview.2
        └── Breaking Change
            └── v2.0.0-preview.1
                └── PR Merge
                    └── v2.0.0
                        └── v2.1.0-preview.1
```

This process ensures version numbers always move forward in a predictable and controlled manner.

### Recommended Release Cadence

A short and consistent release cycle is strongly recommended.

Typical workflow:

```text
New Stable Release
    ↓
Several months of features and fixes
    ↓
Regular promotion to stable releases
    ↓
Evaluate need for breaking changes
    ↓
Begin next major version development
```

In practice:

- Feature releases should be delivered regularly.
- Stable releases should be promoted frequently to avoid large release backlogs.
- Major version development should be planned carefully because it typically slows the release cadence significantly.
- Once major-version work begins, development and validation usually take longer until the next stable major release is ready.

### Benefits

- Maintains a clear separation between stable and future development.
- Allows hotfixes to be released independently of ongoing feature work.
- Prevents breaking changes from entering production prematurely.
- Keeps version numbers progressing forward consistently.
- Supports predictable release management and upgrade paths.

### Preview Back Merge

It is recommended to make a back merge post PR event on MAIN branch where the script attempts to introduce the same PR into development branch unless is encounters conflicts it create a branch and a PR so dev team can see they need to fix that merge.


## Git Post-Commit

Keeping commits clean during branch development is critical. If you see the mess in this repo, ignore it. I work alone, I have zero friends, and I hate authority so much I don't even listen to myself. Don't be like me. Set up a post-commit routine cleanup.


### Installation
You can install the tool using the dotnet CLI:
```bash
dotnet tool install -g DeployItSmart
```

Next, you should set up the post-commit processing script.


`.githooks/post-commit`
```bash
#!/bin/sh
dotnet tool install -g DeployItSmart # Constantly Update Tool
depitsmart LocalTrashCommitCleanerRunner
```

To activate it activate the scripts in `.githooks/post-commit` you must run the command:

`git config core.hooksPath .githooks`

In Git Bash, make the hook executable with `chmod +x .githooks/post-commit`. Git runs it before each push; a nonzero exit from the helper blocks the push, allowing you to address any issues first. Hooks are local to each clone and are not shared automatically, so each contributor must install the hook in their own checkout.


## Visual Studio Code (Important)

Make sure you disable the `Sync Changes` button on VS CODE otherwise it will pull/merge remote before pushing cause new commit merge which will require manual reset to clean the history.

I will however try to make it auto cleaned if that occurs but for now make sure you dont pull dirty changes and force push clean up.

`.vscode/settings.json`:

```json
    "git.showActionButton": {
        "commit": true,
        "publish": false,
        "sync": false,
    }

```


