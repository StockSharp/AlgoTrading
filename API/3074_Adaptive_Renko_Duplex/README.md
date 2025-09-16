# Adaptive Renko Duplex Strategy

## Overview
The **Adaptive Renko Duplex Strategy** is a StockSharp port of the original `Exp_AdaptiveRenko_Duplex.mq5` expert advisor. The converted version keeps the idea of running **two independent Adaptive Renko streams** – one dedicated to bullish setups and another to bearish setups – while exposing the logic through the high-level API. Each stream builds Renko-style support and resistance rails whose brick height dynamically adapts to recent volatility. The strategy reacts to trend reversals detected inside these rails and can maintain asymmetric configurations for the long and short sides.

Unlike classical Renko trading systems, which operate on synthetic bricks, the duplex approach listens to standard candles and continuously recalculates the adaptive Renko buffers. Signals are only generated on fully completed candles to avoid repainting and to match StockSharp's event-driven model.

## Market Data and Indicators
- **Candle subscriptions** – two independent `DataType` parameters select the candle series that feed the long and short Renko streams. They may point to the same timeframe or to different ones.
- **Adaptive Renko reconstruction** – each stream embeds the original indicator logic. A minimum brick size (expressed in points) is compared against `K × volatility` and whichever is larger defines the new brick height. The indicator keeps track of upper/lower envelopes plus colored trend levels (support in uptrends, resistance in downtrends).
- **Volatility sources** – choose between an `AverageTrueRange` or a `StandardDeviation` indicator. Both operate on the candle series used by their respective stream and accept custom lookback lengths.

## Trading Logic
1. **Long-side detection**
   - The long stream builds adaptive bricks using the configured parameters.
   - When the uptrend line (`RenkoTrend.Up`) appears on the delayed bar defined by `LongSignalBarOffset`, the strategy issues a market buy order. The order size is `Volume + |Position|`, enabling immediate reversals from short to long.
   - If a downtrend line is detected after the configured delay and `LongExitsEnabled` is true, all long exposure is closed.
2. **Short-side detection**
   - The short stream mirrors the logic: a `RenkoTrend.Down` signal produces a market sell, while a `RenkoTrend.Up` on the delayed bar exits shorts when `ShortExitsEnabled` is enabled.
3. **Signal delay** – both sides respect their `SignalBarOffset` parameters, reproducing the one-bar shift used by the MetaTrader expert. Setting the offset to zero reacts on the most recent finished candle.
4. **Position sizing** – the StockSharp version relies on the strategy `Volume` property. Always set it before starting the strategy.

## Risk Management
- **Stop-loss / take-profit** – distances are specified in **points** and multiplied by the instrument `PriceStep` to produce absolute prices. Stops are checked whenever a subscribed candle closes. Because StockSharp does not automatically create server-side protective orders, the exits are handled by market orders.
- **State tracking** – the strategy stores the price at which the last long or short entry was executed (based on the candle close) so it can evaluate the distance to the stop or target.
- **Manual overrides** – standard `Stop` or `Protective` modules can be attached on top by calling `StartProtection()` externally if account-level risk management is required.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `LongCandleType` | 4-hour candles | Candle series used to compute long signals. |
| `ShortCandleType` | 4-hour candles | Candle series used to compute short signals. |
| `LongVolatilityMode` | ATR | Volatility source (`AverageTrueRange` or `StandardDeviation`) for long bricks. |
| `ShortVolatilityMode` | ATR | Volatility source for short bricks. |
| `LongVolatilityPeriod` | 10 | Lookback period for the long volatility indicator. |
| `ShortVolatilityPeriod` | 10 | Lookback period for the short volatility indicator. |
| `LongSensitivity` | 1.0 | Multiplier applied to the volatility value before building long bricks. |
| `ShortSensitivity` | 1.0 | Multiplier applied to the volatility value before building short bricks. |
| `LongPriceMode` | Close | Price input (`HighLow` or `Close`) used to update the long Renko rails. |
| `ShortPriceMode` | Close | Price input used to update the short Renko rails. |
| `LongMinimumBrickPoints` | 2 | Minimum brick height for the long stream, measured in points. |
| `ShortMinimumBrickPoints` | 2 | Minimum brick height for the short stream. |
| `LongSignalBarOffset` | 1 | Delay (in bars) before confirming a long signal. |
| `ShortSignalBarOffset` | 1 | Delay (in bars) before confirming a short signal. |
| `LongEntriesEnabled` | true | Toggle to allow or block long entries. |
| `LongExitsEnabled` | true | Toggle to allow or block Renko-driven long exits. |
| `ShortEntriesEnabled` | true | Toggle to allow or block short entries. |
| `ShortExitsEnabled` | true | Toggle to allow or block Renko-driven short exits. |
| `LongStopLossPoints` | 1000 | Stop-loss distance for long positions (points × `PriceStep`). |
| `LongTakeProfitPoints` | 2000 | Take-profit distance for long positions. |
| `ShortStopLossPoints` | 1000 | Stop-loss distance for short positions. |
| `ShortTakeProfitPoints` | 2000 | Take-profit distance for short positions. |

> **Point conversion** – the MQL version used the broker “point” definition. In StockSharp every distance is multiplied by `Security.PriceStep` (or `Security.MinStep` fallback) to convert points into absolute price increments. Adjust the defaults for the tick size of your instrument.

## Usage Guidelines
1. **Configure the environment** – assign `Security`, `Portfolio`, and `Volume` before starting the strategy. Make sure the data source can deliver all configured candle timeframes.
2. **Tailor both streams** – you can keep the default symmetric setup or assign different timeframes/volatility modes to the long and short sides for asymmetric behavior.
3. **Monitor logs** – the strategy emits `LogInfo` messages on every entry and exit, indicating the Renko level that triggered the action. Use these logs to validate that signals match expectations.
4. **Combine with external modules** – additional filters (session control, equity protection, etc.) can be attached via StockSharp’s high-level APIs because the strategy exposes the signals in the main `Strategy` class.
5. **Backtesting considerations** – when testing on historical data, prefer candle builders that can reconstruct the required timeframes so the adaptive Renko remains consistent.

## Differences from the Original Expert Advisor
- MetaTrader-specific features (magic numbers, money-management modes, deviation handling, push notifications) are intentionally omitted. Position sizing relies solely on the StockSharp `Volume` property.
- The original EA placed server-side stop-loss and take-profit orders. The converted version checks the configured distances on every finished candle and closes via market orders.
- Signals are evaluated strictly on completed candles to avoid partial-bar recalculations. This mirrors the `IsNewBar` check used in the MQL implementation.
- The adaptive Renko reconstruction follows the published algorithm but is implemented in C# without creating additional indicator objects, which keeps the update path efficient while respecting StockSharp’s high-level API conventions.

## Recommended Enhancements
- Pair the duplex stream with higher-level regime filters (session schedules, volatility filters) to avoid trading in illiquid conditions.
- Attach trailing-stop modules or equity-based protections via `StartProtection()` for account-level safeguards.
- Log or plot the generated support/resistance rails to visually validate the strategy during discretionary review.
