# ComFracti Fractal RSI Strategy

## Overview
ComFracti Fractal RSI Strategy is a StockSharp port of the MetaTrader expert *ComFracti*. The algorithm searches for directional bias using Bill Williams fractals on two timeframes and filters the signals with a fast RSI calculated on daily candles. Once a valid setup appears, the strategy opens a single position, protects it with configurable stop-loss and take-profit distances, and can optionally exit when the signal reverses or when a holding time limit is reached.

The default configuration replicates the 15-minute trading timeframe with a 1-hour confirmation timeframe and a daily RSI length of three periods using the candle open price, just like the original expert.

## Trading Logic
1. **Fractal bias detection**
   - Finished candles from the trading timeframe and the higher timeframe are processed through a five-bar fractal window.
   - The `Primary*Shift` and `Higher*Shift` parameters define how many bars back the strategy inspects for the latest confirmed fractal. The defaults match the original value of `3`, meaning the code evaluates the fractal that was confirmed three candles ago.
   - A down fractal (swing low) without an accompanying up fractal is treated as bullish (+1). An up fractal without a down fractal is treated as bearish (-1).
2. **Daily RSI filter**
   - A `RelativeStrengthIndex` with the configurable `RsiPeriod` (default `3`) runs on the daily timeframe and uses the candle open price, matching the MetaTrader implementation.
   - Long setups require the RSI to be below `50 - RsiBuyOffset`; short setups require the RSI to be above `50 + RsiSellOffset`.
3. **Entry conditions**
   - **Buy**: Both fractal trackers report +1 and the RSI filter is bullish. The strategy opens a long position if it is flat or short, sending enough volume to flip to the long side.
   - **Sell**: Both fractal trackers report -1 and the RSI filter is bearish. The strategy opens a short position if it is flat or long, sending enough volume to flip to the short side.
4. **Position management**
   - Protective stop-loss and take-profit levels are computed immediately after the position changes based on `StopLossPips` and `TakeProfitPips` multiplied by the instrument pip size.
   - The position can be closed when the price hits the stop or target, when `ExpiryMinutes` elapses, or when `CloseOnOppositeSignal` is enabled and the signal reverses.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `Volume` | Order volume used for every entry. | `0.1` |
| `TakeProfitPips` | Profit target distance in pips. Set to `0` to disable. | `700` |
| `StopLossPips` | Stop-loss distance in pips. Set to `0` to disable. | `2500` |
| `ExpiryMinutes` | Maximum holding time in minutes before forcing an exit. `0` disables the timer. | `5555` |
| `CloseOnOppositeSignal` | Close the active position when the signal flips to the opposite direction. | `false` |
| `PrimaryBuyShift` | Bars back to inspect the bullish fractal on the trading timeframe. | `3` |
| `HigherBuyShift` | Bars back to inspect the bullish fractal on the higher timeframe. | `3` |
| `PrimarySellShift` | Bars back to inspect the bearish fractal on the trading timeframe. | `3` |
| `HigherSellShift` | Bars back to inspect the bearish fractal on the higher timeframe. | `3` |
| `RsiBuyOffset` | Offset below 50 required for long setups. | `3` |
| `RsiSellOffset` | Offset above 50 required for short setups. | `3` |
| `RsiPeriod` | RSI length on the daily timeframe. | `3` |
| `CandleType` | Trading timeframe candle type. | 15-minute candles |
| `HigherTimeFrame` | Confirmation timeframe candle type. | 1-hour candles |
| `DailyTimeFrame` | Candle type used for the daily RSI. | 1-day candles |

## Implementation Notes
- The strategy uses the high-level candle subscription API (`SubscribeCandles().Bind(...)`) and manages indicators internally without exposing them through `Strategy.Indicators`, as required by the guidelines.
- Fractals are computed via an internal helper that stores a rolling five-candle window and updates the signal only after a fractal is confirmed.
- RSI values are retrieved via `RelativeStrengthIndex.Process(...)` with the candle open price, matching the MetaTrader `PRICE_OPEN` mode.
- Only one position is maintained at a time. Market orders flip the position when needed by adding the volume required to cover an existing exposure.
- Pip size is estimated from `Security.PriceStep` and `Security.Decimals`, using a 10x multiplier for assets quoted with three or more decimal places, reproducing the MetaTrader `Point` to pip conversion.

## Usage Tips
- The fractal shifts must be large enough to ensure the requested candle index exists. With a shift of three, the tracker requires at least five finished candles per timeframe before generating signals.
- When trading instruments with different tick sizes (e.g., indices or stocks), adjust `TakeProfitPips` and `StopLossPips` to match the instrument pip definition.
- Disabling `CloseOnOppositeSignal` replicates the original expert adviser behaviour (it was disabled by default) and relies solely on stops, targets, or the expiry timer for exits.
- The strategy does not implement martingale or risk-based sizing; the MetaTrader lot calculation relied on account margin functions that are not available in StockSharp. Use the `Volume` parameter or wrap the strategy in a portfolio manager if dynamic position sizing is required.
