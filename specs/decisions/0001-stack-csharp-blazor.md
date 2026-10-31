# 0001. Stack: C# with ASP.NET Core and Blazor

Date: 31/10/2026. Status: accepted.

## Context

Diapason is a local web app (`localhost` only) that reads `.xlsx` workbooks, computes coverage, margin and forecasts,
and runs on Windows and Debian. The domain logic must stay free of I/O to be tested. The norm prefers C#, bash and
PowerShell 7, with open source dependencies under MIT or Apache 2.0. The first idea was React with Node.

## Decision

- C# on the current .NET LTS, one language end to end.
- ASP.NET Core hosts the app, bound to `localhost`; the UI is Blazor (interactive server rendering), so no separate
  front-end toolchain.
- ClosedXML (MIT) reads the workbooks, only in the import layer.
- The domain is a plain class library with no I/O; tests with xUnit (Apache 2.0).

## Consequences

- One toolchain (`dotnet`) for build, tests and run, the same on Windows and Debian.
- Blazor offers fewer ready-made UI components than React; charts and grids come from a library chosen when the first
  view needs them, under the same licence rule.
- React with Node is rejected: a second language and toolchain with no need that C# cannot cover.
