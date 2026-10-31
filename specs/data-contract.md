# Data contract

Status: accepted (31/10/2026). The layout of the two kinds of workbook Diapason reads: the team registry
(one) and the engagement plans (one per engagement). Both stay outside the repository; the example below is synthetic.

Common rules: one table per sheet, headers in row 1, names exactly as written here; extra columns and sheets are
ignored; dates are calendar dates; amounts in euro; shares as decimals (`0.25` is 25%).

## Team registry

### `People`

| Column | Type | Rule |
|---|---|---|
| `PersonId` | text | unique |
| `DisplayName` | text | |

### `LevelHistory`

| Column | Type | Rule |
|---|---|---|
| `PersonId` | text | exists in `People` |
| `Level` | text | exists in `LevelRates` |
| `ValidFrom` | date | unique per person; a promotion is a new row |

### `LevelRates`

| Column | Type | Rule |
|---|---|---|
| `Level` | text | |
| `ValidFrom` | date | unique per level |
| `ChargeableRate` | amount per hour | > 0 |
| `BusinessDevelopmentRate` | amount per hour | > 0 |
| `ExtraTimeShare` | share | >= 0 |

### `PersonRates` (optional)

The real rates of a person, when known; they win over the rates of the level.

| Column | Type | Rule |
|---|---|---|
| `PersonId` | text | exists in `People` |
| `ValidFrom` | date | unique per person |
| `ChargeableRate` | amount per hour | > 0 |
| `BusinessDevelopmentRate` | amount per hour | > 0 |

### `Calendar`

One calendar for all engagements, so holidays are written once.

| Column | Type | Rule |
|---|---|---|
| `FortnightStart` | date | the 1st or the 16th of a month; unique |
| `WorkingHours` | hours | >= 0, holidays already removed |

## Engagement plan

### `Engagement`

One row.

| Column | Type | Rule |
|---|---|---|
| `EngagementId` | text | unique across all plans |
| `Name` | text | |

### `Areas`

| Column | Type | Rule |
|---|---|---|
| `Area` | text | unique |
| `SoldValue` | amount | >= 0 |

### `Assignments`

One row per assignment; a person can have several.

| Column | Type | Rule |
|---|---|---|
| `AssignmentId` | text | unique in the plan |
| `PersonId` | text | exists in `People`; empty for an open position |
| `Level` | text | only for an open position, exists in `LevelRates` |
| `Area` | text | exists in `Areas` |
| `Role` | text | |
| `Kind` | `Forecast` or `Actual` | |
| `WorkType` | `Chargeable` or `BusinessDevelopment` | |
| `Allocation` | share | > 0 and <= 1 |
| `FirstFortnight` | date | a `FortnightStart` of the calendar |
| `LastFortnight` | date | a `FortnightStart` of the calendar, >= `FirstFortnight` |

## Calculation

For each assignment and each fortnight `f` from `FirstFortnight` to `LastFortnight`:

- `hours = WorkingHours(f) x Allocation`; `days = hours / hours per day` (8, from configuration).
- Level: the open position's `Level`, or the person's `LevelHistory` row in force on the first day of `f`.
- Base rate: the `PersonRates` row in force on the first day of `f`, else the `LevelRates` row of the level in force
  then; `ChargeableRate` or `BusinessDevelopmentRate` by `WorkType`.
- `cost = hours x base rate x (1 + ExtraTimeShare of the level)`.

FTE, cost per day, totals by area, by month and by year are sums of these rows.

## Checks

Every rule in the tables above, plus:

- every fortnight of an assignment has a `Calendar` row and a level and rates in force on its first day;
- the allocation of one person in one fortnight, over all plans and per `Kind`, is at most 1 (a warning, not an error).

An error names workbook, sheet, row and column, and says what to fix; a plan with errors is not loaded.

## Example (synthetic)

Registry: `People` has `P1` Alice Example and `P2` Bruno Sample. `LevelHistory`: `P1` level `L6` from 01/01/2026;
`P2` level `L9` from 01/01/2026 and `L8` from 01/01/2027. `LevelRates` from 01/01/2026: `L6` 90 / 70, extra time 0;
`L8` 60 / 48, extra time 0; `L9` 40 / 32, extra time 0.2. `Calendar`: 01/12/2026 88 hours, 16/12/2026 72 hours.

Plan `E1`, area `Core` sold 50,000. Assignment `A1`: `P2`, `Core`, `Forecast`, `Chargeable`, allocation 0.5, from
01/12/2026 to 16/12/2026.

- 01/12/2026: 88 x 0.5 = 44 hours, 5.5 days, cost 44 x 40 x 1.2 = 2,112.
- 16/12/2026: 72 x 0.5 = 36 hours, 4.5 days, cost 36 x 40 x 1.2 = 1,728.
- From 01/01/2027 the same assignment would be costed at level `L8`.
