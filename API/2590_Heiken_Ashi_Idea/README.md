# Heiken Ashi Idea Strategy

## Overview

The strategy reproduces the behaviour of the original **HeikenAshiIdea.mq4** expert advisor using the StockSharp high-level API. It waits for aligned bullish or bearish signals on two timeframes of Heikin Ashi candles and then places pending limit orders at a configurable distance from the market. The goal is to catch strong continuation moves when the most recent Heikin Ashi candle has no wick against the direction of the trend.

## Trading Logic

1. **Heikin Ashi reconstruction** – the strategy internally rebuilds Heikin Ashi candles for the primary trading timeframe and for a higher confirmation timeframe. For each timeframe the last two Heikin Ashi candles are stored so that the body direction and the presence of wicks can be analysed.
2. **Breakout condition** – a long setup appears when both timeframes show:
   - the most recent Heikin Ashi candle is bullish and its open equals the low (no lower shadow), and
   - the previous Heikin Ashi candle is also bullish but it has a lower shadow.
   A short setup requires the symmetric bearish conditions (no upper shadow on the latest candle and an upper shadow on the previous one).
3. **ATR volatility filter** – the Average True Range with configurable length must be rising (`ATR[t] > ATR[t-1]`) if the filter is enabled. This reproduces the original `ActiveMarket` volatility check.
4. **Trading window** – signals are ignored outside the user defined trading session (default 09:00–19:00).
5. **Order placement** – when a signal is valid the strategy places a single pending limit order:
   - Long signal → buy limit order at `ClosePrice - DistancePoints * PriceStep`.
   - Short signal → sell limit order at `ClosePrice + DistancePoints * PriceStep`.
   Existing opposite pending orders are cancelled before a new order is queued. The strategy tracks only one pending order per direction and automatically clears references when the order becomes inactive.
6. **Position management** – optional take-profit and stop-loss distances are translated into StockSharp protective mechanisms via `StartProtection`. When a new candle of the “close-all” timeframe opens, the strategy cancels all pending orders and closes any open position if the flag is enabled. This mimics the `UseCloseAll` behaviour from the original EA.

## Risk Management

- Protective levels are expressed in price steps (points) to stay close to the MetaTrader implementation. They are optional; using `0` disables the corresponding protection.
- Pending orders are only placed when the calculated distance is positive and the trading volume is above zero.
- The strategy never averages positions automatically; it first flattens the opposite pending order before scheduling a new one.
- A tolerance equal to half of the instrument price step is used when checking if Heikin Ashi candles have or have not wicks. This prevents floating point rounding issues while staying faithful to the original strict comparisons.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `DistancePoints` | Distance in price steps for the pending limit orders. | `8` |
| `StopLossPoints` | Stop-loss distance in price steps (0 disables the stop). | `0` |
| `TakeProfitPoints` | Take-profit distance in price steps (0 disables the target). | `20` |
| `UseCloseAllOnNewBar` | Close position and cancel orders when a new candle of the close-all timeframe opens. | `true` |
| `CandleType` | Primary candle type used for trading signals. | `30m` timeframe |
| `HigherCandleType` | Confirmation candle type for the multi-timeframe filter. | `1d` timeframe |
| `CloseAllCandleType` | Candle type that triggers the close-all routine. | `7d` timeframe |
| `StartHour` | First hour of the trading session (inclusive). | `9` |
| `EndHour` | Last hour of the trading session (inclusive). | `19` |
| `UseAtrFilter` | Enable the ATR rising volatility filter. | `true` |
| `AtrPeriod` | ATR period used by the volatility filter. | `14` |

## Additional Notes

- The strategy uses the built-in `Volume` property from `Strategy` as the base order size. Adjust it before starting the strategy.
- Because the StockSharp implementation uses candle close prices for pending order placement, live execution can differ slightly from the original MT4 code that used bid/ask quotes, but the core idea remains intact.
- To extend the logic for different markets simply tune the candle types, trading window and distance parameters while keeping the multi-timeframe confirmation in place.
