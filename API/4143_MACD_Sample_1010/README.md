# MACD Sample 1010 Strategy

## Overview
This module ports the MetaTrader expert advisor **macd_sample_1010.mq4** to the StockSharp high-level API. The original script combined Bollinger Bands with simple money-management rules: when the close price finished above the upper band plus a configurable buffer it opened a sell order, while a close below the lower band minus the buffer triggered a buy order. Positions were closed once a fixed profit or loss amount (expressed in pips) was reached. The StockSharp version reproduces the same logic by subscribing to the requested candle series, binding a `BollingerBands` indicator, and issuing market orders and position management calls from the candle callback.

The conversion keeps the behaviour of the legacy expert on finished candles. Every evaluation happens when a candle is fully formed, ensuring the breakout and exit decisions match the bar-close processing of the MetaTrader platform. Optional balance-based scaling of the trade volume is also implemented to emulate the `LotIncrease` flag from the MQL4 code.

## Conversion notes
- Uses the high-level `SubscribeCandles` + `Bind` workflow to feed the `BollingerBands` indicator without custom buffers.
- Employs the StockSharp `StrategyParam<T>` infrastructure so that all inputs are visible in the user interface and ready for optimisation.
- Calls `BuyMarket` and `SellMarket` with calculated offsets that respect the instrument’s `PriceStep`, matching the pip-based calculations in MetaTrader.
- Implements the optional lot scaling through `Portfolio.CurrentValue` (with `BeginValue` as a fallback) and caps the resulting volume at 500 lots, just like the original expert.
- Works strictly with completed candles to avoid the tick-by-tick churn that the original script controlled via bar counters.
- Adds descriptive English comments to clarify the intent of each processing block.

## Parameters
| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ProfitTargetPips` | `decimal` | `3` | Number of pips of favourable movement required to close a position in profit. Set to `0` to disable the take-profit rule. |
| `LossLimitPips` | `decimal` | `20` | Number of pips of adverse movement tolerated before the position is liquidated. Set to `0` to disable the stop-loss rule. |
| `BandDistancePips` | `decimal` | `3` | Buffer (in pips) added above the upper band and below the lower band before a breakout is confirmed. |
| `BollingerPeriod` | `int` | `4` | Number of candles used to calculate the Bollinger Bands. |
| `BollingerDeviation` | `decimal` | `2` | Standard deviation multiplier applied by the Bollinger Bands indicator. |
| `BaseVolume` | `decimal` | `1` | Initial trade size, expressed in lots. This value is also used as the baseline for the scaling logic. |
| `LotIncrease` | `bool` | `true` | When enabled, recalculates the trade volume on every candle so it follows the ratio between the current portfolio balance and the starting balance. |
| `OneOrderOnly` | `bool` | `true` | Prevents the strategy from opening a new position when one is already active. When disabled the net position is still managed because StockSharp uses aggregate positions. |
| `CandleType` | `DataType` | `TimeFrame(15m)` | Candle series used for both indicator calculations and trading decisions. |

## Trading logic
1. `OnStarted` creates the Bollinger Bands indicator with the configured period and deviation, subscribes to the `CandleType` data stream, and binds the `ProcessCandle` method.
2. Each finished candle triggers `ProcessCandle`, which recalculates the current trading volume (if `LotIncrease` is active) before evaluating signals.
3. If the close price is greater than the upper band plus `BandDistancePips` (converted to price units with `PriceStep`), the strategy sends a market sell order. If the close price is below the lower band minus the buffer, it sends a market buy order. When `OneOrderOnly` is `true` new entries are only attempted when the net position is zero.
4. After potential entries are processed, the strategy inspects the current position:
   - Long positions are closed once the profit distance reaches `ProfitTargetPips` or when the loss reaches `LossLimitPips`.
   - Short positions are closed when the close price moves in favour by `ProfitTargetPips` or against by `LossLimitPips`.
5. All profit and loss comparisons are performed in price units derived from the symbol’s `PriceStep`, faithfully replicating the pip-based checks in the MetaTrader expert.

## Position sizing logic
- When `LotIncrease` is disabled the strategy trades the constant `BaseVolume` value on every signal.
- When `LotIncrease` is enabled the first candle stores the starting balance per lot (`initial balance / BaseVolume`). Subsequent candles compute the ratio between the current balance and that baseline, round it to one decimal place (mimicking `NormalizeDouble(..., 1)` from MQL4), and clamp the result to a maximum of 500 lots. The computed value is then used as the order volume for the next trade.
- If portfolio information is unavailable the strategy gracefully falls back to the static `BaseVolume`.

## Usage guidelines
1. Attach the strategy to the desired instrument and confirm that the `Security.PriceStep` reflects the pip size you intend to trade.
2. Select the candle timeframe in `CandleType`. The original script was typically executed on intraday timeframes (5–15 minutes), but any bar size can be used.
3. Adjust the band settings, pip offsets, and risk controls to match your trading preferences.
4. Decide whether the position size should scale with the account balance (`LotIncrease`) or remain fixed.
5. Start the strategy. Monitor the log to verify that entries and exits occur on completed candles at the expected price levels.

## Differences from the MetaTrader version
- StockSharp works with aggregated positions, so even when `OneOrderOnly` is disabled the result is a single net position rather than multiple independent tickets.
- The take-profit and stop-loss rules are implemented directly in the strategy instead of registering pending orders with specific price levels, but the resulting behaviour is equivalent because checks occur on every finished candle.
- Logging flags (`logging`, `logerrs`, `logtick`) from the original expert are omitted; StockSharp’s built-in logging already records order and trade events.
- File-based logging and statistics from the MetaTrader version are not recreated because StockSharp exposes richer analytics through portfolios and strategies.
