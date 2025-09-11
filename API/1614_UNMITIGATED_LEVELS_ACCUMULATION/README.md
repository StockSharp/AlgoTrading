# UNMITIGATED LEVELS ACCUMULATION Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Accumulates long positions by placing limit orders at previous day, week, month and year lows that have not been revisited recently. Orders are only placed during the London session and all positions are closed on new all‑time highs.

## Details

- **Entry Criteria**:
  - Limit buys at unmitigated historical lows during session hours.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Close all on new all-time high.
- **Stops**: None.
- **Default Values**:
  - `Max Lookback` = 50
  - `Session Start` = 09:00
  - `Session End` = 17:00
  - `Base PDL` = 0.1
  - `Base PWL` = 0.2
  - `Base PML` = 0.4
  - `Base PYL` = 0.8
- **Filters**:
  - Category: Mean Reversion
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: Yes (London session)
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
