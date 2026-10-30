# Diapason

A local web app to see whether a team's revenue covers its cost, and what margin is left.

## Goal

Give a team lead one place to answer: does what the team sells cover what the team costs, what margin remains, how
the team is shaped by career level, what a promotion would change, and how actuals compare with budget and forecast.

## Description

Diapason reads one yearly Excel workbook and shows coverage, margin, headcount and forecasts for a team of about 40
people. It runs only on `localhost`: per-person costs are personal data, so nothing goes to the cloud and no real data
ever enters this repository (see "Rules").

Modules, in delivery order:

1. **Coverage and margin**: sold vs. cost, by engagement and in total.
2. **Headcount and pyramid**: people by career level.
3. **Promotion simulator**: the cost and margin effect of moving someone up a level.
4. **Forecast vs. actual**: budget, rolling forecast and estimate at completion; variances split into volume, price,
   mix and cost.

## Technologies

To be decided (see "To do"). Any choice must run on Windows and Debian, locally, read `.xlsx`, and keep the domain
logic free of I/O so it can be tested.

## Dependencies

| From | What it uses | Type |
|---|---|---|
| Yearly Excel workbook | sheets `Levels`, `People`, `Engagements`, `Allocations`, `Actuals`, `Budget` | data file, kept outside the repo |

Nothing depends on Diapason.

## Architecture

```
codename-diapason/
|-- README.md         what the project is
|-- AGENTS.md         rules for coding assistants: common rules, then the product's own
|-- CLAUDE.md         shell for Claude Code (@AGENTS.md)
|-- CONTRIBUTING.md   how to contribute (specs first, GitHub Flow)
|-- LICENSE           MIT
|-- SECURITY.md       how to report a vulnerability
|-- CODE_OF_CONDUCT.md
|-- specs/            brainstorming.md; architecture as it comes
|   |-- features/     one folder per feature: spec.md, plan.md with its tasks
|   \-- decisions/    ADRs
|-- src/              source code
|-- tests/            tests, from the acceptance criteria
|-- tools/            deterministic tools for agents and people
|-- .githooks/        pre-commit: gitleaks on staged files
|-- .github/          CI (gitleaks), issue and PR templates, Dependabot
|-- .gitleaks.toml    secret scan rules
|-- .gitignore        keeps workbooks, exports and local data out of git
\-- .editorconfig     shared editor settings
```

Four layers (to be detailed in `specs/architecture.md`): import (workbook to domain types), domain (pure calculations, one module per
feature), API, UI. Cost rule: a standard cost per career level; when a person's real cost is known, it wins.

## Rules

- **No real data in the repository, ever**: no workbooks, names, costs or levels of real people, not even in notes or
  test fixtures. Tests use synthetic data only.
- **Local only**: the app binds to `localhost`; no telemetry, no external calls.

