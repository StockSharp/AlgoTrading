# Aeron Robot Grid Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements a grid-based hedging system inspired by the AeronRobot expert advisor. It places buy and sell orders at predefined price intervals and increases position volume after each new order. The approach seeks to capture small price oscillations while controlling risk through configurable take-profit, stop-loss and trade limits.

The strategy works with both long and short positions. When price moves in steps defined by the *Gap* parameter, a new order is opened with volume multiplied by *LotsFactor*. Profits are secured when price returns by *TakeProfit* points, and losses are cut if the move reaches *StopLoss* points. The *Hedging* flag allows maintaining positions on both sides simultaneously.

## Details

- **Entry Criteria**:
  - **Long**: price falls by `Gap` points from the last buy price.
  - **Short**: price rises by `Gap` points from the last sell price.
- **Volume Management**: volume of each new order is multiplied by `LotsFactor`.
- **Exit Criteria**:
  - positions on a side are closed when profit exceeds `TakeProfit` points.
  - positions on a side are closed when loss exceeds `StopLoss` points.
- **Parameters**:
  - `FirstLot` – initial order volume.
  - `LotsFactor` – multiplier for subsequent orders.
  - `Gap` – base distance between grid levels in points.
  - `GapFactor` – multiplier that expands the gap after each trade.
  - `MaxTrades` – maximum number of trades per side.
  - `Hedging` – allow simultaneous long and short positions.
  - `TakeProfit` – target in points.
  - `StopLoss` – protective limit in points.
  - `CandleType` – candle timeframe used for processing.
- **Long/Short**: both.
- **Filters**:
  - Category: Grid / Mean reversion
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High

