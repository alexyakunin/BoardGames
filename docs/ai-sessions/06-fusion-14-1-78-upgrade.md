# Session 06

**User:** Bump Fusion to 14.1.78. It's a fix release — MessagePack 3.1.8
crashes its own source generator and analyzer on a closed generic struct
coming from a referenced assembly, which is the shape `PropertyBag` got in
Fusion 14.1.71, and any affected assembly silently lost its generated
MessagePack resolver. Verify no `CS8785`/`AD0001` appear in the build.

**Opus5:** Bumped `<ActualLabFusionVersion>` in `src/Directory.Packages.props`
from 14.1.73 to 14.1.78 — a one-line diff, no code changes needed since the
release has no breaking changes and the wire format is unchanged.
`dotnet build BoardGames.sln` succeeds with 0 errors across all 7 projects,
and a full rebuild reports zero `CS8785` and zero `AD0001`. Confirmed csc
actually receives the MessagePack 3.1.8 source generator as an analyzer, so
the absence is a real signal rather than the generator never running. A
baseline rebuild against 14.1.73 was also clean: BoardGames has no
`PropertyBag` member in any of its MessagePack contracts, so it was never hit
by the bug — this bump is a regression guard here, not a fix confirmation.
