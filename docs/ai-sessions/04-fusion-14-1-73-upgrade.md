# Session 04

**User:** Commit and push the pending Fusion 14.1.73 upgrade: the
`ActualLabFusionVersion` bump in `src/Directory.Packages.props`, and the
removal of the `CodeKeeper.Keep<UnitMessagePackFormatter>()` block (plus
its comment and the two now-unused usings) from `src/UI/Program.cs` —
`ActualLab.Core` retains that formatter itself as of 14.1.73. Verify
`dotnet build BoardGames.sln` still succeeds first.

**Opus5:** Verified the build, then committed the version bump and the
`Program.cs` cleanup together as the 14.1.73 upgrade and pushed to `main`.
