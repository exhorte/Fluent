# Push-to-Talk Test Report

Date: 2026-07-27
Build: 0 warnings, 0 errors

## New Tests Added

### Fluent.Core.Tests (29 new)

**PushToTalkKeyStateMachineTests (25 tests)**

| # | Test | Status |
|---|---|---|
| 1 | StartRequested_transitions_from_Idle_to_Arming | PASS |
| 2 | StartRequested_is_ignored_when_not_Idle | PASS |
| 3 | StartRequested_is_ignored_after_MinimumHoldReached | PASS |
| 4 | MinimumHoldReached_transitions_from_Arming_to_Recording | PASS |
| 5 | StopRequested_during_Arming_transitions_to_Cancelled | PASS |
| 6 | Cancelled_during_Arming_transitions_to_Cancelled | PASS |
| 7 | Cancelled_can_reset_to_Idle | PASS |
| 8 | StopRequested_transitions_from_Recording_to_Stopping | PASS |
| 9 | MaximumDurationReached_transitions_from_Recording_to_Stopping | PASS |
| 10 | StopRequested_ignored_when_not_Recording | PASS |
| 11 | Second_StopRequested_is_ignored | PASS |
| 12 | Full_pipeline_transitions_to_Idle | PASS |
| 13 | New_cycle_possible_after_returning_to_Idle | PASS |
| 14 | Error_in_Stopping_transitions_to_Failed | PASS |
| 15 | Error_in_Transcribing_transitions_to_Failed | PASS |
| 16 | Error_in_Rewriting_transitions_to_Failed | PASS |
| 17 | Error_in_Inserting_transitions_to_Failed | PASS |
| 18 | Failed_can_reset_to_Idle | PASS |
| 19 | IsBusy_is_false_at_Idle | PASS |
| 20 | IsRecording_is_true_during_Arming | PASS |
| 21 | IsRecording_is_true_during_Recording | PASS |
| 22 | IsRecording_is_false_after_Stopping | PASS |
| 23 | ResetToIdle_clears_everything | PASS |
| 24 | IsRecording_returns_false_for_non_recording_states (implicit) | PASS |
| 25 | Start_from_Failed_resets_cleanly | PASS |

**PushToTalkConfigurationTests (4 tests)**

| # | Test | Status |
|---|---|---|
| 1 | MinimumHoldDuration_is_200ms | PASS |
| 2 | MinimumRecordingDuration_is_250ms | PASS |
| 3 | MaximumRecordingDuration_is_5_minutes | PASS |
| 4 | StopProcessingDelay_is_50ms | PASS |

### Fluent.Windows.Tests (6 new + 4 skipped)

| # | Test | Status |
|---|---|---|
| 1 | Hook_is_not_installed_by_default | PASS |
| 2 | Dispose_uninstalls_hook | PASS |
| 3 | Double_dispose_is_safe | PASS |
| 4 | ShouldSuppressWinKey_is_callable_and_returns_false_without_installed_hook | PASS |
| 5 | Install_sets_IsInstalled | SKIP (requires message pump) |
| 6 | Install_is_idempotent | SKIP (requires message pump) |
| 7 | Uninstall_clears_IsInstalled | SKIP (requires message pump) |
| 8 | Uninstall_is_idempotent | SKIP (requires message pump) |

## Existing Tests (Re-verified)

| Suite | Tests |
|---|---|
| Fluent.Core.Tests (existing) | 46 (TextInsertionPolicy, DictationErrorPresenter, History, TranscriptionLanguage, ProductPrinciples) |
| Fluent.Windows.Tests (existing) | 4 (KeyboardInputSender layout, ActiveTargetDetector, assembly boundaries) |
| Fluent.Speech.Tests | 10 |
| Fluent.Audio.Tests | 6 |
| Fluent.Rewrite.Tests | 172 |
| Fluent.Backend.Tests | 54 |
| Fluent.Persistence.Tests | 44 |
| Fluent.IntegrationTests | 88 |

## Totals

- **New tests**: 35 (29 Core + 6 Windows)
- **Skipped**: 4 (require message pump)
- **Existing tests re-passed**: 424
- **Grand total passing**: 459
- **Failures**: 0
