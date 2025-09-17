# GlamTrader Strategy

The **GlamTrader Strategy** is a StockSharp high-level API conversion of the MetaTrader expert advisor `GlamTrader.mq5`. The original robot blends a shifted moving average with the Laguerre RSI oscillator and the Awesome Oscillator to filter momentum before opening a single market position. The port preserves the exact decision tree and money-management rules while adapting order execution, charting, and risk controls to StockSharp conventions.

## How the strategy works

1. Subscribe to the candle series defined by `CandleType` (M15 by default). The selected timeframe feeds every indicator.
2. Build a configurable moving average on the chosen `AppliedPrice` source and shift it by `MaShift` bars to reproduce the displaced buffer used in MetaTrader.
3. Recreate the Laguerre RSI filter internally using the four-stage recursive filter (`LaguerreGamma` controls the smoothing factor). The value remains in the `[0;1]` range like the original custom indicator.
4. Calculate the Awesome Oscillator with standard 5/34 simple averages of the median price and store the current and previous readings for slope detection.
5. Only when no position is open:
   - **Long entry** – moving average above the current close, Laguerre RSI above `0.15`, and Awesome Oscillator rising versus the previous bar.
   - **Short entry** – moving average below the current close, Laguerre RSI below `0.75`, and Awesome Oscillator falling versus the previous bar.
6. On entry the strategy converts stop-loss/take-profit distances from pips into price offsets using the instrument tick size. Distances are adjusted for 3- or 5-digit quotes exactly like `Point * 10` in MQL.
7. While a position is active the algorithm mirrors the original trailing routine: once price advances more than `TrailingStopPips + TrailingStepPips`, the stop is trailed to `TrailingStopPips` behind (or above) the market. Exits are executed when the candle range touches the trailing stop or the take-profit price.

## Entry and exit logic

- Always keep at most one position. Opposite signals are ignored until the current trade is closed.
- Long trades require a bearish displaced moving average (price crossing above the line), Laguerre RSI leaving the oversold band (`> 0.15`), and increasing Awesome Oscillator momentum.
- Short trades require a bullish displaced moving average (price crossing below the line), Laguerre RSI falling out of the overbought band (`< 0.75`), and decreasing Awesome Oscillator momentum.
- Stops and targets are enforced with price comparisons against candle highs/lows so intra-bar hits are respected even though the logic runs on finished candles.
- Trailing follows the MetaTrader rule: the stop only moves after price advances by the stop distance plus the trailing step and never reverts.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(15).TimeFrame()` | Timeframe used for indicator calculations and decision making. |
| `TradeVolume` | `decimal` | `1` | Volume used for market orders. |
| `StopLossBuyPips` | `decimal` | `50` | Stop-loss distance in pips for long entries. |
| `TakeProfitBuyPips` | `decimal` | `50` | Take-profit distance in pips for long entries. |
| `StopLossSellPips` | `decimal` | `50` | Stop-loss distance in pips for short entries. |
| `TakeProfitSellPips` | `decimal` | `50` | Take-profit distance in pips for short entries. |
| `TrailingStopPips` | `decimal` | `5` | Trailing stop distance in pips. Set to zero to disable trailing. |
| `TrailingStepPips` | `decimal` | `15` | Additional profit (in pips) required before the trailing stop can move. |
| `MaPeriod` | `int` | `14` | Moving average lookback length. |
| `MaShift` | `int` | `1` | Positive displacement applied to the moving average. |
| `MaMethod` | `MaMethod` | `LinearWeighted` | Moving average type (simple, exponential, smoothed, or linear weighted). |
| `AppliedPrice` | `AppliedPrice` | `Weighted` | Price source used for both the moving average and Laguerre filter. |
| `LaguerreGamma` | `decimal` | `0.7` | Laguerre smoothing coefficient (0–1 range). |

## Usage tips

1. Attach the strategy to the desired security, ensure the broker model supplies tick size/step information, and set `CandleType` to match the timeframe you want to trade.
2. Adjust pip-based risk parameters to instrument volatility. The conversion automatically normalizes distances using `PriceStep`; five-digit FX symbols get the expected 10× multiplier.
3. Optional chart helpers draw the moving average on the price area and plot the Awesome Oscillator in a separate pane together with your own trades.
4. Start the strategy. It will manage stops and trailing internally, mirroring the `OpenBuy`, `OpenSell`, and trailing routines from the original MQL code.

## Notes

- The Laguerre RSI implementation mirrors the `laguerre.mq5` indicator, including the `CU/(CU+CD)` normalization.
- Awesome Oscillator values come from StockSharp's built-in indicator, so no manual buffer copying is required.
- Because the logic is evaluated on completed candles, backtests and live trading remain deterministic and free from tick-level repainting.
