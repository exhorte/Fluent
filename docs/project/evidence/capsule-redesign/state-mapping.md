# Capsule Redesign — State Mapping

## Business → Visual Mapping

```
PushToTalkState          → CapsuleVisualState
────────────────────────────────────────────
Idle                     → Idle
Cancelled                → Idle
Arming                   → Recording
Recording                → Recording
Stopping                 → Processing
Transcribing             → Processing
Rewriting                → Processing
Inserting                → Processing
Failed                   → Error
```

## Implementation

`CapsuleStateMapper.Map(PushToTalkState)` is a pure static function with no dependencies. Unknown states fall back to `Idle`.

Located at: `src/Fluent.App/Models/CapsuleStateMapper.cs`

## Test Coverage

All 10 enum values + 1 unknown fallback = 11 test cases. Verified in `tests/Fluent.IntegrationTests/CapsuleStateMapperTests.cs`.
