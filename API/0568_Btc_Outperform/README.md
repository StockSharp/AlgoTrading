# BTC Outperform Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Compares weekly and quarterly closing prices. Goes long when the weekly price is higher than the quarterly price, and goes short when the quarterly price is higher.

## Details
- **Entry Criteria:**
  - **Long:** weekly close > quarterly close.
  - **Short:** quarterly close > weekly close.
- **Long/Short:** Both.
- **Exit Criteria:** Reverse signal.
- **Stops:** None.
- **Default Values:** Weekly = 7 days, Quarterly = 90 days.
