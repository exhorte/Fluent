# Fluent Visual Language

Status: IMPLEMENTATION_BASELINE

Date: 2026-07-14

## Reference Inventory

| Reference | Dimensions | Product use |
| --- | ---: | --- |
| `docs/templates/is_not_recording.png` | 197 x 82 | Idle capsule composition |
| `docs/templates/is_recording.png` | 162 x 88 | Recording capsule and seven-bar waveform |
| `docs/templates/Screenshot 2026-07-14 194203.png` | 1198 x 605 | Primary dashboard shell and Overview hierarchy |
| `docs/templates/Screenshot 2026-07-14 194219.png` | 1190 x 619 | Future dictionary information architecture |
| `docs/templates/Screenshot 2026-07-12 015407.png` | 1249 x 482 | Secondary functional inventory only |
| `docs/templates/vibrant-sound-wave-animation.jpg` | 1244 x 700 | Fluent accent palette and motion rhythm |

The capsule PNG files include an opaque black canvas and BridgeVoice branding. They remain read-only design references and are not embedded in the application. Fluent reconstructs the visual language with DPI-independent WPF shapes, text, gradients, and animations.

## Priority And Adaptation

1. The two compact PNG files define capsule geometry and state contrast.
2. The July 14 Overview capture defines the dashboard shell, spacing, card hierarchy, and sidebar.
3. The July 14 Dictionary capture informs a later dictionary phase; it does not authorize dictionary behavior in this slice.
4. The waveform image contributes cyan-to-violet color and bar rhythm.
5. The July 12 capture is an older, lower-density layout and is not the primary geometry reference.

BridgeVoice-specific words, subscription controls, macOS window furniture, sample records, counts, synchronization claims, and account UI are deliberately excluded. Runtime branding is `Fluent`; navigation is French and future destinations are visibly unavailable until implemented.

## Design Tokens

- Window: `#050608`
- Sidebar: `#090B0F`
- Card: `#0D1016`
- Selection: `#171B24`
- Border: `#242A36`
- Primary text: `#F4F6FA`
- Secondary text: `#9299A6`
- Muted text: `#5D6573`
- Cyan: `#30D1CB`
- Blue: `#639BFA`
- Violet: `#7A55E7`
- Success: `#65D6A6`
- Warning: `#E7B867`
- Error: `#F06A76`

Segoe UI Variable is the primary Windows type family. Page titles use 24 DIP, metric values 20 DIP, normal content 13 DIP, and compact labels 10 DIP.

## Floating Capsule

- Fixed transparent host so state changes do not jump horizontally.
- Near-black pill, graphite one-pixel border, fully rounded ends.
- Idle: native Fluent gradient mark plus `Fluent`; this is an availability state, not a pause/resume command.
- Recording: mark, separator, and seven animated light bars. The animation indicates active recording state; it does not claim to measure microphone amplitude.
- Processing: mark, short status, and a bounded three-dot pulse. No recording waveform is visible after stop.
- Every transition removes the previous controllable Storyboard before starting the next one.
- The window remains non-focusable, non-activating, topmost, and excluded from the taskbar.

## Dashboard Overview

The first dashboard uses a 40-DIP top bar, a compact dark sidebar, 24-to-28-DIP content margins, low-contrast cards, and thin separators. It displays only verified live information already available in the application: hotkey state, dictation state, target state, local engine profile, privacy behavior, and the latest result.

History, Dictionary, Profiles, and Settings appear only as `À venir` navigation context. This phase creates no records, metrics, suggestions, persistence, accounts, telemetry, or cloud behavior.

## Future Components

The language is intended to support `SidebarNavItem`, `MetricCard`, `SectionCard`, `TranscriptRow`, `DictionaryEntryRow`, `SearchBox`, `TagChip`, `ConfidenceBadge`, `NyxLogoControl`, and `AudioWaveformControl`. Only the components needed by the current truthful Overview and capsule are implemented now.
