## Why Promotion Strategy

Promotion Strategy is one of my favorite release models. While it may be unnecessary for small packages, it provides an excellent release flow and preview control model for larger projects.

It is similar to trunk-based development but instead centered around devlopment branch. Small, short-lived branches are merged into `main-dev` and are automatically released as the next preview version (`preview.1`, `preview.2`, and so on).

What I like most about this approach is that it avoids polluting version history with hundreds of patch releases while keeping stable releases highly reliable. By promoting changes through controlled stages, the risk of introducing major regressions into stable releases is significantly reduced.

This strategy is fully supported here, but it requires additional discipline and process overhead compared to simpler workflows.

## Rules

1. **Never** allow a minor or major change to be merged directly into `main`.
   - Zero-tolerance policy.

2. The **only** direct merges allowed into `main` are:
   - Hotfixes
   - Changes that result in a patch version increase only

3. Always back-merge hotfixes from `main` into `main-dev`.

4. Keep release cycles as short as reasonably possible.
   - A common criticism of promotion-based workflows is that long-lived release branches eventually drift too far from development, creating maintenance challenges.
   - While this concern is valid, strict promotion rules reduce the impact because `main` only receives hotfixes or promoted changes from `main-dev`.
   - Short development and promotion cycles still provide the best results.

5. Understand the versioning flow:
   - Every preview promotion should increase the **minor** version at least once.
   - Every stable release should increase the **patch** version at least once.

## What You'll Find Here

The examples in this folder demonstrate a simple but robust workflow optimized for fast execution and deployment.

The primary goals are:

- Minimize process overhead
- Maintain a reliable and predictable release pipeline
- Provide full `main-dev` support
- Support automatic back-merging of hotfixes

## Enable Back-Merge Support

To allow GitHub Actions to create automatic back-merge pull requests:

1. Navigate to **Repository Settings**
2. Open **Actions** → **General**
3. Enable **"Allow GitHub Actions to create and approve pull requests"**


## Useful
You can put this in your github project readme to inform contributor of convention used by your project and whether or not you have all the proper implementation for supporting `trace!:`
```markdown

## Commit Convention
To Keep branch history clean you must use [DeployItSmart](https://github.com/mshimshon/DeployItSmart) Standard Convention.

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

HIGHLY RECOMMENDED: Committers should use the post commit script on their local computer on the repos itself to auto-clean wip

Create file `.git/hooks/post-commit`

\```bash
#!/bin/sh
dotnet tool install -g DeployItSmart
depitsmart LocalTrashCommitCleanerRunner
\```

Execute in shell to activate the git hook on that local project `git config core.hooksPath .git/hooks`.

Note: this only activate locally each user on each repos must activate it this way unless you setup a global hook which in not recommended.

Note 2: it is of your responsability to ensure clear branch history prior to merge whether or not you are using the post-commit.

Check out [DeployItSmart](https://github.com/mshimshon/DeployItSmart) for more details.

```
