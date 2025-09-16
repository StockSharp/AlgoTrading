# Roman Direction Flip
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy recreates the original MQL expert advisor that was published as `roman.mq5`. It always keeps a position open and alternates the trade direction only after the previous trade is closed. While the position remains profitable it repeats the same direction; after a stop-loss the strategy flips to the opposite side. The StockSharp version works with level 1 data and uses best bid/ask quotes to emulate the pip-based exits from MetaTrader.

## Strategy logic
1. **Initial direction** – at start-up the `StartWithBuy` parameter defines whether the very first order is a buy or a sell. The decision is stored in `_nextTradeBuy` so it persists between deals.
2. **Entering the market** – when the strategy is flat and there are no pending orders, it submits a market order in the predefined direction. For buy orders the current best ask is stored as the reference entry price, and for sell orders the current best bid is used. This mirrors the MetaTrader implementation where buys execute at the ask and sells at the bid.
3. **Monitoring the open position** – after the order is filled, the strategy listens to level 1 updates. Each update provides the latest bid/ask so the algorithm can calculate the unrealized profit expressed in price steps (pips). The security `PriceStep` is used as the denominator, and a fallback of `1` is applied if the step is unknown.
4. **Take-profit rule** – when the unrealized gain reaches or exceeds `TakeProfitSteps`, the position is closed with `ClosePosition()`. The `_nextTradeBuy` flag keeps the same value so the next order will follow the direction that just succeeded.
5. **Stop-loss rule** – when the unrealized loss reaches or exceeds `StopLossSteps`, the position is closed and `_nextTradeBuy` is toggled. The following trade therefore enters in the opposite direction, matching the original EA behavior where the `bs` boolean flips on a loss.
6. **Order throttling** – `_orderPending` prevents the algorithm from submitting multiple orders while a previous request is still being processed. The flag is reset in `OnPositionChanged` after the position size is updated.

This simple sequence keeps the strategy invested at all times and alternates direction only after a losing trade. As a result the system resembles a trend-following toggle: after a stop-loss it assumes the trend has changed and follows the new side.

## Parameters
- `OrderVolume` *(decimal, default = 0.1)* – quantity sent with each market order. Set this to the contract size you need for live trading or simulations.
- `TakeProfitSteps` *(int, default = 46)* – positive number of price steps required to trigger the take-profit. Steps correspond to `Security.PriceStep`, so on a symbol with a 0.01 tick size the default equals 0.46 price units.
- `StopLossSteps` *(int, default = 31)* – maximum adverse price movement (in steps) before the position is closed and the direction is flipped.
- `StartWithBuy` *(bool, default = true)* – determines whether the very first trade is long (`true`) or short (`false`). Subsequent trades depend on the results of prior positions.

Every parameter is exposed through `StrategyParam<T>`, supports optimization (except the boolean switch), and is visible in the UI thanks to `SetDisplay` metadata.

## Data and execution details
- Subscribes to `SubscribeLevel1()` to receive best bid/ask quotes. No candle or indicator data is required.
- Uses `BuyMarket`/`SellMarket` for entries and `ClosePosition()` for exits, ensuring the logic remains close to the MQL version that relied on immediate market orders.
- Stores the last known bid/ask locally to mimic the `_Point`-based profit calculation from MetaTrader.

## Risk management
- Fixed take-profit and stop-loss in price steps guarantee that every trade has pre-defined exit levels.
- The direction flip after a loss can lead to rapid alternation in choppy markets, so position size (`OrderVolume`) should be calibrated according to account risk tolerance.
- Because the strategy almost always holds a position, it is sensitive to overnight gaps and sudden quote jumps; consider external safeguards if that is a concern.

## Default values
- `OrderVolume` = 0.1
- `TakeProfitSteps` = 46
- `StopLossSteps` = 31
- `StartWithBuy` = true

## Filters
- **Category**: Trend following / direction toggle
- **Direction**: Both (long & short)
- **Indicators**: None
- **Stops**: Yes (fixed step take-profit and stop-loss)
- **Complexity**: Basic
- **Timeframe**: Tick / Level1 quotes
- **Seasonality**: No
- **Neural networks**: No
- **Divergence**: No
- **Risk level**: High (always in market)

## Notes
- The original EA stored the next direction in a boolean named `bs`. The StockSharp port keeps the same idea via `_nextTradeBuy` while adding order throttling to avoid duplicate submissions.
- Price step granularity matters: if your instrument uses fractional pips, adjust the defaults so that profit/loss targets reflect the desired monetary amounts.
