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
