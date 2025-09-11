# Prop Firm Business Simulator
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that simulates prop firm risk management using Keltner Channel breakouts with position sizing based on risk per trade.

The method places stop orders at the channel boundaries. Quantity is calculated so that the distance between bands represents the chosen percentage of account equity.

## Details

- **Entry Criteria**: Price breaks Keltner Channel bands.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite band breakout.
- **Stops**: Yes.
- **Default Values**:
  - `MaPeriod` = 20
  - `AtrPeriod` = 10
  - `Multiplier` = 2m
  - `RiskPerTrade` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Keltner, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
