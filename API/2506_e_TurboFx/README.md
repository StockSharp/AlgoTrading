# e-TurboFx Strategy

## Overview
The **e-TurboFx** strategy is a momentum exhaustion reversal system originally written for MetaTrader 5. It monitors the most recent finished candles and looks for sequences where the candle bodies keep expanding in the same direction. A growing series of bearish candles indicates capitulation and therefore a potential long setup, while a growing series of bullish candles announces a possible short opportunity. The StockSharp port uses the high-level API with candle subscriptions and automated position protection.

## Trading logic
- Inspect the last `DepthAnalysis` finished candles of the selected `CandleType`.
- Count how many consecutive candles closed below their open (bearish) and how many closed above their open (bullish).
- Track body size progression: every new candle in the sequence must have a larger absolute body than the previous one. When this condition fails the sequence is reset.
- **Long entry:** `DepthAnalysis` consecutive bearish candles with strictly expanding bodies trigger a market buy, provided no position is currently open.
- **Short entry:** `DepthAnalysis` consecutive bullish candles with strictly expanding bodies trigger a market sell, again only when the position is flat.
- While a position is active the strategy pauses signal detection to avoid stacking trades. Risk management is delegated to the built-in protection block configured at start.

## Position management
- `StartProtection` automatically registers stop-loss and take-profit orders using distances measured in price steps (exchange ticks). Setting a distance to zero disables the corresponding protective order.
- The strategy keeps only one open position. When a new signal appears after the previous trade is closed, the candle sequences are rebuilt from scratch based on fresh market data.
- Market entries use the `TradeVolume` parameter. Changing the parameter in the UI immediately updates the strategy volume.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `DepthAnalysis` | Number of recent finished candles used to validate the expansion pattern. Higher values demand longer streaks before trading. | `3` |
| `TakeProfitSteps` | Take-profit distance in exchange price steps (ticks). `0` disables the take profit. | `120` |
| `StopLossSteps` | Stop-loss distance in exchange price steps (ticks). `0` disables the stop-loss. | `70` |
| `TradeVolume` | Order volume sent with each market entry. | `0.1` |
| `CandleType` | Candle data type (time frame) subscribed for the analysis. | `1 hour` time frame |

All numeric parameters have optimization metadata so they can be included in StockSharp optimizations if desired.

## Notes and recommendations
- The original MQL5 expert adviser recalculated candle data on every tick; the StockSharp implementation achieves the same behaviour with finished candle events and internal counters.
- Because the strategy relies on candle body comparisons, it is sensitive to the selected timeframe. Shorter timeframes will produce more signals but may require tighter stops.
- Ensure that the connected security exposes a valid `PriceStep` so that stop-loss and take-profit distances defined in steps translate correctly to prices.
- Before running in live trading, validate the behaviour in the Designer/Backtester to confirm that the stop and target distances align with the chosen instrument.
