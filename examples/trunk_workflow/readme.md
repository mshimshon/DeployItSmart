## Why Trunk-Based Development?

Trunk-based development is a divisive strategy (yes, I'm one of *those* people), but when managed correctly it can be extremely effective, especially for SaaS projects with varying levels of complexity behind the deployment process.

In its purest form, trunk-based development generally places little emphasis on elaborate versioning schemes. A new feature ships? Increment the minor version. A bug fix ships? Increment the patch version. Whether the version is `1.2.3` or `1.2.111` is often irrelevant compared to the speed and reliability of delivery.

Solo projects naturally fit this model. You work with short-lived branches, merge quickly into `main`, and keep development moving forward. that's usually all there is to it.

This tool is designed around that philosophy. Pushing to `main` automatically produces a minimal patch release unless the commit message contains `skipci~`.

## What You'll Find Here

The examples in this folder demonstrate a simple but robust workflow optimized for fast execution and deployment. The goal is to minimize process overhead while maintaining a reliable release pipeline.

It uses no development promotion branch pipeline at all.
