# BTC DCA AHR999 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy buys Bitcoin each Monday between the configured start and end
dates. The amount invested depends on the AHR999 index which combines a
geometric mean of price with a logarithmic growth model for Bitcoin.

## Details

- **Entry Criteria**:
  - On Mondays inside the date range if AHR999 < 0.45 buy `UsdInvest2` amount.
  - On Mondays inside the date range if AHR999 < 1.2 buy `UsdInvest1` amount.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Positions are held; no automatic exit logic is included.
- **Stops**: None.
- **Default Values**:
  - UsdInvest1 = 100.
  - UsdInvest2 = 1000.
  - Length = 200.
  - Start date = 2024-02-01, End date = 2025-12-31.
- **Filters**:
  - Category: Accumulation.
  - Direction: Long.
  - Indicators: AHR999.
  - Stops: No.
  - Complexity: Medium.
  - Timeframe: Daily.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Medium.
