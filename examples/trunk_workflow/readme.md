## Why Trunk-Based Development?

Trunk-based development is a divisive strategy (yes, I'm one of *those* people), but when managed correctly it can be extremely effective, especially for SaaS projects with varying levels of complexity behind the deployment process.

In its purest form, trunk-based development generally places little emphasis on elaborate versioning schemes. A new feature ships? Increment the minor version. A bug fix ships? Increment the patch version. Whether the version is `1.2.3` or `1.2.111` is often irrelevant compared to the speed and reliability of delivery.

Solo projects naturally fit this model. You work with short-lived branches, merge quickly into `main`, and keep development moving forward. that's usually all there is to it.

This tool is designed around that philosophy. Pushing to `main` automatically produces a minimal patch release unless the commit message contains `skipci~`.

## What You'll Find Here

The examples in this folder demonstrate a simple but robust workflow optimized for fast execution and deployment. The goal is to minimize process overhead while maintaining a reliable release pipeline.

It uses no development promotion branch pipeline at all.


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
