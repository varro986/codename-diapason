# Diapason

A local web app to see whether a team's revenue covers its cost, and what margin is left.

## Goal

Give a team lead one place to answer: does what the team sells cover what the team costs, what margin remains, how
the team is shaped by career level, what a promotion would change, and how actuals compare with budget and forecast.

## Description

Diapason reads one Excel workbook per engagement, each with a life of its own, in a common format with its forecast
and its actuals, and shows coverage, margin, headcount and forecasts for a team of about 40
people. The team registry and all engagement workbooks sit in one data folder, shared by the team lead and the
executives; each runs the app on their own machine, and the app only reads that folder, never writes it. The UI is in
Italian (default) and English. It runs only on `localhost`: per-person costs are personal data, so nothing goes to the cloud and no real data
ever enters this repository (see "Rules").

The plan is kept by fortnight: a calendar gives the working hours of each fortnight (holidays included), and
allocation times hours gives hours, days (hours / 8) and cost. Monthly and yearly views are sums of fortnights.

Modules, in delivery order:

1. **Coverage and margin**: sold vs. cost and margin, by area, by engagement and in total, for forecast and actual.
2. **Headcount and pyramid**: people by career level, and the team by role, skill, location and main client.
3. **Promotion simulator**: the cost and margin effect of moving someone up a level, in named scenarios copied from
   the plan, compared side by side and kept as an archive.
4. **Forecast vs. actual**: per engagement, forecast and actual over time (FTE, cost, days, cost per day, delta and
   cumulative curves), what remains and the margin; budget, rolling forecast and estimate at completion; variances
   split into volume, price, mix and cost.

## Technologies

C# on the current .NET LTS: ASP.NET Core on `localhost`, Blazor UI, ClosedXML to read `.xlsx`, xUnit for tests;
Windows and Debian. Why: `specs/decisions/0001-stack-csharp-blazor.md`.

## Dependencies

| From | What it uses | Type |
|---|---|---|
| Data folder | the team registry (people, level history, rates, calendar) and one `.xlsx` per engagement; layout in `specs/data-contract.md` | shared data folder, kept outside the repo |

Nothing depends on Diapason.

## Architecture

```
codename-diapason/
|-- README.md         what the project is
|-- AGENTS.md         rules for coding assistants: common rules, then the product's own
|-- CLAUDE.md         shell for Claude Code (@AGENTS.md)
|-- CONTRIBUTING.md   how to contribute (specs first, dev then main)
|-- LICENSE           MIT
|-- SECURITY.md       how to report a vulnerability
|-- CODE_OF_CONDUCT.md
|-- Diapason.slnx     the solution; Directory.Build.props holds the settings shared by every project
|-- specs/            brainstorming.md, data-contract.md
|   |-- features/     one folder per feature: spec.md, plan.md with its tasks
|   \-- decisions/    ADRs
|-- src/
|   |-- Diapason.Domain/     types of the data contract, rules, costing, economics; no I/O
|   |-- Diapason.Workbooks/  reads and writes .xlsx (ClosedXML), loads the data folder
|   |-- Diapason.Web/        ASP.NET Core + Blazor on localhost, texts in Resources/ (it, en)
|   \-- Diapason.Tool/       command line for templates and synthetic data
|-- tests/            one test project per src project, from the acceptance criteria
|-- tools/            new-templates.ps1, new-sample-data.ps1
|-- cicd/             run.ps1: the checks run before every push and in CI
|-- .claude/          settings.json: permissions of coding assistants
|-- .githooks/        pre-commit: gitleaks on staged files
|-- .github/          CI (gitleaks; cicd/run.ps1 on pull requests), issue and PR templates, Dependabot
|-- .gitleaks.toml    secret scan rules
|-- .gitignore        keeps workbooks, exports and local data out of git
\-- .editorconfig     shared editor settings
```

Flow: the web app asks `Diapason.Workbooks` to load the data folder; each workbook becomes domain types plus the
issues of its cells, the domain checks the rules of the data contract, costs every assignment fortnight by fortnight and
sums sold, cost and margin by area, engagement and total. A workbook with errors is left out and its errors are shown
with workbook, sheet, row and column. Cost rule: two rates per career level, chosen by the
type of work (chargeable or business development), each raised by the extra-time share of the level; when a person's
real rates are known, they win.

People come from one team registry that every engagement plan references by person id. It has, among others: people
(one row each, stable attributes), level history (person, level, valid from: a promotion is a new row) and rates (per
level, and optionally per person, each with its valid-from date). A fortnight is costed with the level and rates in
force on its first day, so past actuals never change and a simulated promotion is a future row in a scenario.

## Rules

- **No real data in the repository, ever**: no workbooks, names, costs or levels of real people, not even in notes or
  test fixtures. Tests use synthetic data only.
- **Local only**: the app binds to `localhost`; no telemetry, no external calls.

## Usage

Requires the .NET 10 SDK.

- Run with the synthetic data: `pwsh -NoProfile -File tools/new-sample-data.ps1` (writes `data/sample/`, ignored by
  git), then `dotnet run --project src/Diapason.Web` and open `http://localhost:5188`.
- Run on the real data folder: set `DIAPASON_DATA_FOLDER` to the shared folder (or `Diapason:DataFolder` in
  `src/Diapason.Web/appsettings.json`) and start the app the same way. The app only reads the folder; "Ricarica" /
  "Reload" reads it again after a workbook is changed.
- Empty templates to fill in: `pwsh -NoProfile -File tools/new-templates.ps1 -Folder <folder>`.
- Settings, section `Diapason` of `appsettings.json`: `DataFolder`, `RegistryFileName`, `HoursPerDay`, `Port`.

## Tests

`pwsh -NoProfile -File cicd/run.ps1`: build with warnings as errors, `dotnet format` check, all tests. One test:
`dotnet test --filter "FullyQualifiedName~CostingTests"`. CI runs the same script on every pull request.
