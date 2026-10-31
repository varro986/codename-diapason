# 001. Coverage and margin

From: module 1 of the README; IDEA-14 (coverage by area), IDEA-7 (a minimal synthetic team), and the owner's answers
of 31/10/2026. Data layout and calculation: `specs/data-contract.md`.

## Stories

- As a team lead or an executive, I open the app and see, for every engagement and in total, the sold value, the
  forecast and actual cost, the margin and the coverage, by area.
- As the team lead, when I change a workbook, I press "Reload" and see the new numbers or what to fix, with file,
  sheet, row and column.
- As the team lead, I generate empty templates of the registry and of a plan, to fill in or to paste today's data into.
- As a developer, I generate synthetic workbooks to test and develop with, never real ones.

## Definitions

- `cost` is the sum of the cost rows of the data contract, per `Kind` (forecast, actual).
- `margin = sold - cost`; `margin % = margin / sold`, empty when sold is 0.
- `coverage = sold / cost`, empty when cost is 0.

## Acceptance criteria

1. **Cost of an assignment**: Given the synthetic example of the data contract, when the plan is loaded, then the
   forecast of `Core` has 80 hours, 10 days and a cost of 3,840.
2. **Level in force**: Given a person promoted on the 1st of a month, when an assignment spans that date, then the
   fortnights before it use the old level's rates and the ones from it the new level's.
3. **Person rates win**: Given a person with rates in `PersonRates` in force, when a fortnight is costed, then those
   rates are used, raised by the extra-time share of the person's level.
4. **Open position**: Given an assignment with a `Level` and no `PersonId`, when it is costed, then the rates of that
   level are used.
5. **Margin and coverage**: Given an area sold 50,000 with a forecast cost of 40,000, then the margin is 10,000, the
   margin % is 20% and the coverage is 1.25; with sold 0 the margin % is empty, with cost 0 the coverage is empty.
6. **Totals**: Given several areas and engagements, then the engagement total is the sum of its areas and the overall
   total is the sum of the engagements, for sold, cost and margin, per `Kind`.
7. **Plan with errors**: Given a plan breaking a rule of the data contract, when the folder is loaded, then that plan
   is not shown, the other plans are, and each error names workbook, sheet, row, column and what to fix.
8. **Registry with errors**: Given a registry with an error, when the folder is loaded, then no plan is shown and the
   registry's errors are.
9. **Over-allocation**: Given a person allocated more than 1 in one fortnight over all plans, for one `Kind`, then a
   warning names the person, the fortnight and the plans, and the numbers are still shown.
10. **Folder**: Given the data folder from configuration or `DIAPASON_DATA_FOLDER`, then the registry file and every
    other `.xlsx` of the folder are read, subfolders and `~$` lock files skipped; a missing folder or registry is an
    error that says which path was looked for.
11. **Read only**: Given any load, then no file in the data folder is created, changed or deleted.
12. **Reload**: Given the app running, when "Reload" is pressed, then the folder is read again and the page shows the
    new state; at start-up the folder is read once.
13. **Languages**: Given the app, then it starts in Italian and a selector switches it to English; numbers and dates in
    the Italian format in both.
14. **Local only**: Given the app, then it listens only on `localhost`.
15. **Templates**: Given the templates command, then it writes an empty registry and an empty plan, with the sheets
    and headers of the data contract, into a folder given on the command line, never overwriting an existing file.
16. **Synthetic data**: Given the sample-data command, then it writes a registry and plans with invented people that,
    once loaded, give the numbers of criterion 1, plus one plan with errors for criterion 7.
