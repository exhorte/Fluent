# Phase 04A Accepted Baseline Before Phase 05A

Captured before Phase 05A source mutation on 2026-07-15.

- Git HEAD: `3eebf5de943dc108114da65745b1ea526434ed2a`
- The working tree already contains accepted, uncommitted Phase 03 and Phase 04 changes and must not be reset.

| SHA-256 | File |
| --- | --- |
| `8E855FB93C1CA814DB6C06D38E90645DCF8B1F5324A75BE3376790ECE4E69151` | `src/Fluent.App/MainWindow.xaml` |
| `B81B9CBBD3F11641A44381F9C959A3A474E1C643A6FE7908B11690EFDCFCD0C1` | `src/Fluent.App/MainWindow.xaml.cs` |
| `27760BD4B288F8602E07F8C3D5C9C4D945F22B78DE58D9BA412ACE5B084FEEAF` | `src/Fluent.Rewrite/SafeProfileRewriteService.cs` |
| `22280C51FFDE58813F6473FABB0B59EC23FFA8E81C2CA2811BA4726D7249431D` | `src/Fluent.Rewrite/Validation/RewriteOutputValidator.cs` |
| `201AAF5BACBAE8ACCF1960BF65B989965DD02926EC6502711A2C417C6AC04FD2` | `tests/Fluent.Rewrite.Tests/ProfessionalFrenchRewriteTests.cs` |

Rollback must remove only the Phase 05A dictionary page, navigation, and pipeline call while preserving these accepted baselines.

