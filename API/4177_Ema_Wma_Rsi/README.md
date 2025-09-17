# EMA WMA RSI

## Overview
EMA WMA RSI is a conversion of the MetaTrader 4 expert advisor "EMA WMA RSI" created by cmillion. The original robot compares an exponential moving average (EMA) and a linear weighted moving average (WMA) calculated from candle opens, and filters every crossover with a Relative Strength Index (RSI) threshold. The StockSharp port keeps the same indicator logic, operates on finished candles, and reproduces the money-management options: optional counter-position flattening, point-based stop-loss/take-profit levels, and a trailing stop that can follow fixed distances, the latest fractal, or recent candle extremes.

The strategy is designed for a single symbol and time frame selected via the `Candle Type` parameter. It assumes MetaTrader "points" (the minimal tick) when converting risk distances to absolute prices, so instrument metadata such as `Security.Step` and `Security.StepPrice` should be filled for best results.

## Strategy logic
### Indicators
* **EMA** – period defined by `EMA Period`, applied to candle open prices.
* **WMA** – period defined by `WMA Period`, also fed with candle opens.
* **RSI** – `RSI Period`, calculated on the same open-price stream.

All indicators update once per finished candle. The port mirrors the original "bar open" execution by storing the EMA/WMA values from the previous bar and comparing them against the current bar immediately after it closes.

### Entry rules
* **Long setup**
  1. Current EMA value is below the WMA, while the previous bar had EMA above WMA (a downward cross).
  2. RSI value is above 50.
  3. If a short position exists, it is optionally closed when `Close Counter Trades` is enabled; otherwise the signal is ignored until the strategy is flat.
  4. When the conditions hold, a market buy order is sent using either the fixed volume or the risk-based sizing.
* **Short setup** – symmetrical logic: EMA crosses above WMA, the previous bar showed EMA below WMA, RSI is below 50, and the strategy either flattens a long or skips the trade.

### Exit rules
* **Initial protection** – `Stop Loss (points)` and `Take Profit (points)` translate to absolute distances using the instrument tick size. Either value can be set to zero to disable it.
* **Trailing stop**
  * If `Trailing Stop (points)` is greater than zero, the stop follows price at a fixed distance measured from the latest close (only tightening, never loosening).
  * If the trailing distance is zero, the algorithm searches for adaptive levels:
    * `Trailing Source = CandleExtremes` looks back through previous candle highs/lows. A long stop moves to the first low at least five points below the current price; a short stop uses highs five points above.
    * `Trailing Source = Fractals` scans previously confirmed Bill Williams fractals (two candles on each side). The same five-point buffer applies to avoid placing the stop too close to the current price.
  * Trailing adjustments only activate after price moves beyond the original entry price, reproducing the MetaTrader EA behaviour.
* **Position exit** – When the trailing stop or take-profit is touched within a candle’s range, the position is closed with a market order and the internal state is reset.

### Position sizing
* `Fixed Volume` provides the exact market order size (lots/contracts). This is the default, matching the EA parameter `Lot`.
* Setting `Fixed Volume` to zero activates risk-based sizing. The strategy estimates the monetary risk per unit using the available stop distance (either the configured stop loss or the effective trailing distance) and `Security.StepPrice`. `Risk %` determines how much portfolio equity is exposed per trade. If both fixed volume and risk percent are zero the signal is ignored.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `EMA Period` | Period of the exponential moving average applied to candle opens. | `28` |
| `WMA Period` | Period of the linear weighted moving average on opens. | `8` |
| `RSI Period` | RSI length used as a directional filter. | `14` |
| `Stop Loss (points)` | Stop-loss offset in MetaTrader points. `0` disables the protective stop. | `0` |
| `Take Profit (points)` | Take-profit offset in points. `0` disables the target. | `500` |
| `Trailing Stop (points)` | Fixed trailing distance in points. `0` switches to adaptive trailing (fractals or candle lows/highs). | `70` |
| `Trailing Source` | Adaptive trailing method: `CandleExtremes` for raw highs/lows, `Fractals` for Williams fractals. | `CandleExtremes` |
| `Close Counter Trades` | Close an opposite position before opening a new trade. | `false` |
| `Fixed Volume` | Market order volume. Set to `0` to enable risk-based sizing. | `0.1` |
| `Risk %` | Percent of portfolio equity committed when `Fixed Volume` is zero. Requires a valid stop distance. | `10` |
| `Candle Type` | Primary timeframe used for indicators and signal evaluation. | `30-minute candles` |

## Implementation notes
* Price-step conversions rely on `Security.Step` (or `Security.PriceStep`) and `Security.StepPrice`. Provide realistic instrument metadata to keep point-to-price calculations accurate.
* The strategy processes only finished candles and uses their open prices for indicator updates, matching the "new bar" logic in the MQL4 code.
* Trailing levels keep at least a five-point buffer away from the current price just like the original helper function `SlLastBar`.
* When counter-position closing is disabled, the strategy never hedges—only a single net position is managed at a time.
* No Python implementation is included in this package.
