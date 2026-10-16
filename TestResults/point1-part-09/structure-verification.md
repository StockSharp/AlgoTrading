# Part 09 — complete structural inventory verification

**Source:** all 316 exact strategy keys in TZ-09.md (not an inclusive ID range).

**Method:** retrieved GitHub recursive trees for each relevant API range (`3401-3500`, `3501-3600`, `3601-3700`, `3701-3800`, `3801-3900`). All five trees reported `truncated: false`. Matched each listed key by exact folder name and checked that it has exactly one C# source under `CS/`, exactly one Python source under `PY/`, and every README file below.

## Result

| Check | Observed |
| --- | ---: |
| Unique assigned strategy keys | 316 |
| Keys with one C# file | 316 |
| Keys with one Python file | 316 |
| Keys with all seven README files | 316 |
| Missing or duplicate implementation files | 0 |
| Missing translated README files | 0 |
| Total source/README files implied by these checks | 2844 |

Seven required README names: `README.md`, `README_ru.md`, `README_zh.md`, `README_es.md`, `README_de.md`, `README_pt.md`, `README_ja.md`.

The parameter names for each of the seven README files for **3410**, **3413** and **3433** were also checked against their published parameter lists (zero missing names); the `3467` CandleType default is `5m` in all seven translations.

## Important limitation

**This is a file-structure and name-presence check only.** No statement here certifies numerical defaults, translation semantics, independent trade signals, executed volumes, actual fills, stop execution, runnable C#/Python compilation, or TDD red/green results. These remain mandatory requirements for completing TZ-09.

The current execution environment has no .NET SDK and no local packaged BTC/TON archive. No GitHub Actions run or commit statuses were available on this branch at the time of the check. Do not interpret the structural inventory as 316 completed behavioral audits.
