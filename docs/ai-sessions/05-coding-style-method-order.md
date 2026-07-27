# Session 05

**User:** Fusion's `CODING_STYLE.md` just gained a `#### Method order within
a section` subsection under `### Member Ordering`. Replicate the same rule
here: a callee never goes above its caller; where there's no call
relationship the higher-level method wins; pure utilities sink to the bottom
of their section; public methods run roughly in order of use. Adapt it to
this document rather than pasting it, and replace the Fusion-internal
example if it doesn't read well here.

**Opus5:** Added the subsection at the same spot — right after the
member-ordering list, before the RPC-API list — with the rule text kept
verbatim, and swapped the `ConsolidatingComputed<T>` example for
`ConnectFourEngine.Move` calling `IsWin`, which is the same
caller-above-helper shape in local code.
