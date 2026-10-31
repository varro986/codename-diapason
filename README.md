# Diapason

A local web app to see whether a team's revenue covers its cost, and what margin is left.

## Goal

Give a team lead one place to answer: does what the team sells cover what the team costs, what margin remains, how
the team is shaped by career level, what a promotion would change, and how actuals compare with budget and forecast.

## Description

Diapason reads one Excel workbook per engagement, each with a life of its own, in a common format with its forecast
and its actuals, and shows coverage, margin, headcount and forecasts for a team of about 40
people. It runs only on `localhost`: per-person costs are personal data, so nothing goes to the cloud and no real data
ever enters this repository (see "Rules").

The plan is kept by fortnight: a calendar gives the working hours of each fortnight (holidays included), and
allocation times hours gives hours, days (hours / 8) and cost. Monthly and yearly views are sums of fortnights.

Modules, in delivery order:

1. **Coverage and margin**: sold vs. cost, by engagement and in total.
2. **Headcount and pyramid**: people by career level.
3. **Promotion simulator**: the cost and margin effect of moving someone up a level.
4. **Forecast vs. actual**: budget, rolling forecast and estimate at completion; variances split into volume, price,
   mix and cost.

## Technologies

C# on the current .NET LTS: ASP.NET Core on `localhost`, Blazor UI, ClosedXML to read `.xlsx`, xUnit for tests;
Windows and Debian. Why: `specs/decisions/0001-stack-csharp-blazor.md`.

## Dependencies

| From | What it uses | Type |
|---|---|---|
| Engagement workbooks | one `.xlsx` per engagement, layout in `specs/data-contract.md` | data files, kept outside the repo |
| Team registry | one local `.xlsx`: people, level history and rates, each dated; layout in `specs/data-contract.md` | data file, kept outside the repo |

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
|-- specs/            brainstorming.md; architecture as it comes
|   |-- features/     one folder per feature: spec.md, plan.md with its tasks
|   \-- decisions/    ADRs
|-- src/              source code
|-- tests/            tests, from the acceptance criteria
|-- tools/            deterministic tools for agents and people
|-- cicd/             local checks run before every push (added with the first code)
|-- .claude/          settings.json: permissions of coding assistants
|-- .githooks/        pre-commit: gitleaks on staged files
|-- .github/          CI (gitleaks), issue and PR templates, Dependabot
|-- .gitleaks.toml    secret scan rules
|-- .gitignore        keeps workbooks, exports and local data out of git
\-- .editorconfig     shared editor settings
```

Four layers (to be detailed in `specs/architecture.md`): import (workbook to domain types), domain (pure calculations, one module per
feature), API, UI. Cost rule: two rates per career level, chosen by the
type of work (chargeable or business development), each raised by the extra-time share of the level; when a person's
real rates are known, they win.

People come from one team registry that every engagement plan references by person id. It has three tables: people
(one row each, stable attributes), level history (person, level, valid from: a promotion is a new row) and rates (per
level, and optionally per person, each with its valid-from date). A fortnight is costed with the level and rates in
force on its first day, so past actuals never change and a simulated promotion is a future row in a scenario.

## Rules

- **No real data in the repository, ever**: no workbooks, names, costs or levels of real people, not even in notes or
  test fixtures. Tests use synthetic data only.
- **Local only**: the app binds to `localhost`; no telemetry, no external calls.

