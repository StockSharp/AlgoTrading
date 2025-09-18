# Basic Martingale EA 3

## Overview
The **Basic Martingale EA 3** strategy replicates the MetaTrader 5 expert advisor that combines a trend filter based on the Triple Exponential Moving Average (TEMA) with ATR-driven martingale averaging. The converted StockSharp version keeps the same risk parameters, trading window and money management logic while exposing everything through strategy parameters for optimisation.

## Trading logic
1. **Signal generation** – on every completed candle of the selected timeframe the close price is compared with the TEMA value. A close above the indicator opens a long basket, whereas a close below it opens a short basket. Only one direction can be active at the same time.
2. **Trading window** – new baskets are allowed only between `StartHour` and `EndHour` (exchange time). If both hours are the same the window is considered always open. Set `TradeAtNewBar` to `true` to limit new baskets to one per candle, similar to the original `TradeAtNewBar` switch in MT5.
3. **Averaging grid** – once a position exists the strategy measures the distance from the worst/ best entry price. Whenever the market moves by at least `GridMultiplier × ATR`, an additional order is added in the direction defined by `Averaging` (average-down or average-up) until `MaxAverageOrders` is reached. The new order size follows the chosen martingale mode (`Multiply` or `Increment`).
4. **Protective exits** – optional stop-loss and take-profit levels are inherited from the first order in the basket. In addition the trailing block mimics the MT5 implementation: after `TrailingStart` points of profit the stop is moved to `price - TrailingStop` (or `price + TrailingStop` for shorts) and tightened by `TrailingStep`.
5. **Flattening** – if any stop, take-profit or trailing level is touched the whole basket is closed at market and all averaging counters are reset.

## Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | H1 time-frame | Candle series that drives the strategy. |
| `StartVolume` | `decimal` | `0.01` | Initial volume for the first order in a basket. |
| `StopLossPoints` | `decimal` | `20` | Stop-loss distance in price steps. Set to `0` to disable. |
| `TakeProfitPoints` | `decimal` | `20` | Take-profit distance in price steps. Set to `0` to disable. |
| `StartHour` | `int` | `3` | Hour (inclusive) when new baskets can start. |
| `EndHour` | `int` | `18` | Hour (exclusive) when basket creation stops. |
| `TemaPeriod` | `int` | `50` | Length of the TEMA indicator. |
| `BarsCalculated` | `int` | `3` | Number of finished candles required before trading begins. |
| `AtrPeriod` | `int` | `14` | Period of the Average True Range indicator. |
| `GridMultiplier` | `decimal` | `0.75` | ATR multiplier that defines the grid spacing. |
| `MaxAverageOrders` | `int` | `3` | Maximum number of averaging orders per direction (including the initial one). |
| `Averaging` | enum | `AverageDown` | Choose between averaging in drawdown, averaging in profit, or disabling extra entries. |
| `Martin` | enum | `Multiply` | Select between multiplicative or incremental martingale sizing. |
| `LotMultiplier` | `decimal` | `1.5` | Factor used by the `Multiply` martingale mode. |
| `LotIncrement` | `decimal` | `0.1` | Additional volume used by the `Increment` martingale mode. |
| `TradeAtNewBar` | `bool` | `false` | Restrict new baskets to one per finished candle. |
| `TrailingStart` | `int` | `100` | Profit in points required to activate trailing. |
| `TrailingStop` | `int` | `50` | Trailing stop distance in points. |
| `TrailingStep` | `int` | `30` | Minimum improvement (points) before moving the trailing stop again. |

## Conversion notes
- The StockSharp version keeps the MT5 indicator set-up (TEMA(50) + ATR(14)) and exposes the `bar` parameter as `BarsCalculated`, ensuring at least the specified number of candles before trading.
- Volume handling honours the instrument’s `MinVolume`, `MaxVolume` and `VolumeStep`, so live trading respects exchange limits even with fractional martingale steps.
- Trailing logic follows the original break-even plus trailing-step behaviour but is implemented with aggregated position data because StockSharp positions are netted by instrument.
- Chart annotations from the MT5 expert were not ported because StockSharp already provides order and position visualisation on the chart panels.
