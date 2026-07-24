# Residual Risks

| ID | Risk | Level | Mitigation |
| --- | --- | --- | --- |
| RR-001 | Live Claude Code hook execution was not observed inside a fresh Claude Code session. | Medium | Restart/open Claude Code in this repo and run a small permission smoke test. |
| RR-002 | Official docs can evolve after Claude Code 2.1.207. | Low | Re-check docs before modifying settings schema. |
| RR-003 | PowerShell hooks are deterministic but not a substitute for future product security tests. | Medium | Phase 01 must test real Windows focus/target behavior. |
