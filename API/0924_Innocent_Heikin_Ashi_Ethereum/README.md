# Innocent Heikin Ashi Ethereum Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy goes long on Ethereum when a sequence of bearish candles under the EMA50 is followed by a bullish candle above the EMA50. A stop loss is placed at the lowest low of the recent 28 bars and a take profit is calculated with the `RiskReward` multiplier. Optional **Moon Mode** permits entries above the EMA200. The position may close early on sell or trap signals.

## Details

- **Entry Criteria**:
  - **Long**: at least `ConfirmationLevel` red candles below EMA50 followed by a green candle above EMA50.
  - **Aggressive**: if `EnableMoonMode` is true and price is above EMA200.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Stop loss at the lowest low of the last 28 bars.
  - Take profit using `RiskReward` multiplier.
  - Optional sell or trap signals for early exit.
- **Stops**: Yes.
- **Default Values**:
  - `RiskReward` = 1.
  - `ConfirmationLevel` = 1.
  - `EnableMoonMode` = true.
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
