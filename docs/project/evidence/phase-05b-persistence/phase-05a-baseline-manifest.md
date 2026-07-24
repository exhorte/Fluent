# Phase 05A Accepted Baseline Before Phase 05B

Captured before Phase 05B source mutation on 2026-07-16.

- Git HEAD: `3eebf5de943dc108114da65745b1ea526434ed2a`
- The working tree already contains accepted, uncommitted Phase 03, Phase 04A, and Phase 05A changes and must not be reset.
- Phase 05A user smoke was accepted on 2026-07-16.

| SHA-256 | File |
| --- | --- |
| `54D8C827E85372D654E3C9EFE8A5D2776A30C4F25C3F316C6A94B377F8EB2F03` | `Directory.Packages.props` |
| `61E3F2D043E37F0DB4BE1D8083DD31BEBBFBB8FDE2CF027327DB2556C16BC385` | `src/Fluent.Core/Fluent.Core.csproj` |
| `6733B54789D836FD0C37F679CB437BE24BAC37C8DBF309BA7C600529E06DF7FC` | `src/Fluent.Persistence/Fluent.Persistence.csproj` |
| `B7AB1652846E9B7485AE0387BB13E520F27E048545DED417F466F659839239B2` | `src/Fluent.Persistence/FluentPersistenceAssembly.cs` |
| `7618355183A8EFD23D854B3DE032E684956820271E228FFF8DD0C5B814F48D47` | `src/Fluent.Rewrite/Dictionary/SessionDictionary.cs` |
| `E5C4CC2609103026E907DB9A29BBF44BBD856044E9425B30769B9224238D401A` | `src/Fluent.Rewrite/Dictionary/PersonalDictionaryEntry.cs` |
| `A0D995D74C240B652BD3433C9A364383673081319CA1E68B94F275519ECAB1E0` | `src/Fluent.App/MainWindow.xaml` |
| `EC8F2260CA572BA2F487A65CF28B4D368E68E87BF9514CFD4B20F4B03BB8F9D5` | `src/Fluent.App/MainWindow.xaml.cs` |
| `57874A4C74FE83C36F0452371663C7A6A97FA1B5829D05DC11DAD3DA51CC04F0` | `src/Fluent.App/Views/DictionaryPage.xaml` |
| `06FE24F8476E56809BF04CFC2BB6835D902253D2BAE451E5EC680BADA3696679` | `src/Fluent.App/Views/DictionaryPage.xaml.cs` |
| `DCE31836866161D21088BF9CCF77DE70F38D12BE726CF1C60C149014EBA4FEB1` | `tests/Fluent.Persistence.Tests/Fluent.Persistence.Tests.csproj` |
| `9A63449591A6A42A3C22B70E5D088973C03D53E80C02E07CF6E33A7D151A0E24` | `tests/Fluent.Persistence.Tests/PersistenceAssemblyBoundaryTests.cs` |
| `6C6991917F160C6BC59A571D21C42841E96915CA825A33A0312DAB0ECE99D074` | `tests/Fluent.Rewrite.Tests/PersonalDictionaryTests.cs` |
| `FDB4730047F169A307BF81B1FF805F5788073D8A31F8FAA09C8DF908D8AD0F6F` | `tests/Fluent.IntegrationTests/Fluent.IntegrationTests.csproj` |

Rollback must remove only Phase 05B persistence, dependency, UI-copy, integration, and test changes while preserving this accepted baseline. An existing user database must never be deleted automatically.

