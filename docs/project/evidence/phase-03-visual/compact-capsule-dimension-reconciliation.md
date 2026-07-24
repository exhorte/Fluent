# Compact Capsule Dimension Reconciliation

Status: PASS_RECONCILED_REBUILT_RETESTED

Date: 2026-07-15

## Facts

- The original approved implementation plan and first static review used a 144 by 44 DIP host.
- The current XAML was subsequently adjusted to 140 by 40 DIP before the user's final acceptance; its file timestamp is 2026-07-15T13:57:41+02:00.
- At 14:01 on the same day, after the instructed compact drag and two-cycle smoke, the user confirmed `c'est bon on peut continuer`.
- The 140 by 40 DIP source is therefore retained as the final user-accepted product dimension rather than being overwritten by the earlier plan value.

## Corrected Layout Calculation

- Window: 140 by 40 DIP.
- Border after 2 DIP outer margin: 136 by 36 DIP.
- Content after 1 DIP border and 8 by 5 DIP padding: 118 by 24 DIP.
- Idle logo: 24 DIP, exact vertical fit.
- Recording logo: 26 DIP, nominal 1 DIP extension into padding on each side; no visible clipping in the user smoke.
- Processing logo: 22 DIP, fits with 1 DIP vertical clearance on each side.

## Verification Requirement

The earlier Release build and test timestamps preceded the final XAML adjustment. They were replaced on 2026-07-15 by a fresh Release build with 0 warnings and 0 errors and a complete test run with 35 passed, 0 failed, and 0 skipped.
