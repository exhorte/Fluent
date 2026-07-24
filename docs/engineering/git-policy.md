# Git Policy

Authority: ADR-0007.

- Local Git operations (`status`, `diff`, `log`, `add`, `commit`, local branch) are R0 — automatic.
- A non-force `git push` to a work branch and a draft pull request are R1 — PROJECT_DIRECTOR standing authority, no user prompt. A secret scan of the staged index is mandatory before any push.
- Destructive commands remain denied by the deterministic floor: `git push --force` / `-f` / `--force-with-lease`, `git reset --hard`, destructive `git clean`, history rewrite (`filter-repo`, `filter-branch`).
- Do not modify `.git/` directly.
- Direct pushes to the default branch and public brand publication remain R3 (user required) unless already authorized in the active phase contract.
