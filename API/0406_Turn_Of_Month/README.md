# Turn Of Month
[Русский](README_ru.md) | [中文](README_cn.md)

This seasonal pattern buys equity indices a few days before month‑end and exits shortly after the new month begins, aiming to capture the "turn‑of‑the‑month" effect.

The system stays in cash outside of this window to reduce exposure.

## Details

- **Data**: Daily index levels.
- **Entry**: Buy N days before month end.
- **Exit**: Sell M days after month start.
- **Instruments**: Equity index futures or ETFs.
- **Risk**: Flat outside scheduled window.

