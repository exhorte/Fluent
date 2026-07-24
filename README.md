# Fluent

Fluent est une application Windows locale de dictée française : enregistrement au microphone, transcription locale, réécriture prudente par profil et insertion sécurisée dans le champ initial. L’audio n’est pas enregistré par défaut et le Cloud reste optionnel.

## Status

Phase 07B : le parcours OAuth Google principal est validé par le retest utilisateur ; les contrôles manuels de restauration, hors-ligne et déconnexion restent requis avant clôture. La réécriture Cloud reste optionnelle et indisponible sans backend déployé.

## Prerequisites

- Windows
- PowerShell 7 preferred; Windows PowerShell 5.1 compatibility is maintained where reasonable
- Git
- Claude Code 2.1.x
- .NET SDK 10.0.x

## Structure

- `.claude/`: Claude Code agents, skills, hooks, schemas, templates, and runtime notes
- `docs/`: product, architecture, engineering, project, ADR, and evidence documents
- `src/`: .NET source projects
- `tests/`: .NET test projects
- `benchmarks/`: future speech benchmark workspace
- `tools/`: manual harness workspace

## Verified Commands

The actual commands executed during bootstrap are recorded in `docs/project/BOOTSTRAP_REPORT.md`.

## Claude Code Usage

Read `CLAUDE.md` first. Mutable work requires an active execution contract and Judge approval. The Development Judge is read-only; deterministic hooks enforce security before model judgment.

## Security

Fluent est local-first, n’a pas de télémétrie, n’enregistre pas l’audio par défaut et interdit l’insertion automatique dans les champs de mot de passe. Les secrets, certificats, bases locales, captures audio et modèles sont ignorés par Git.

## Current Limits

- The source tree is physically renamed to Fluent, but the technical rename is not yet reconciled in Git; do not treat the current worktree as clean.
- The harness validates deterministic policy locally; live Claude Code hook behavior still requires use in a fresh Claude Code session.
- No package publishing, deployment, remote PR, or git push has been performed by the current audit task.

## Next Phase

Complete the remaining Phase 07B manual session checks. The proposed next product phase is 07D — authenticated Gemini rewrite, subject to a separate approved contract and external deployment authority.
