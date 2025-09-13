# Martini Martingale Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements a hedged martingale grid. It starts by placing stop orders on both sides of the current price and doubles position size in the opposite direction whenever the market moves against the current exposure by a specified step. All trades are closed once accumulated profit exceeds the target.

## Details

- **Entry Criteria**:
  - Place a buy stop above and a sell stop below the market at distance `Step`.
  - When an order triggers, cancel the opposite stop.
- **Position Management**:
  - Track the price of the last executed order.
  - If price moves against the open position by `Step * orderCount`, send a market order in the opposite direction with double the previous volume.
- **Exit Criteria**:
  - Close all positions when unrealized profit reaches `ProfitClose`.
- **Long/Short**: Both.
- **Stops**: Uses stop orders for initial entries; no stop-loss.
- **Indicators**: None.
- **Filters**: None.

### Parameters

- `Step` – price step in absolute units.
- `ProfitClose` – profit threshold to close all trades.
- `InitialVolume` – starting volume for the first order.
- `CandleType` – candle series used for price updates.
