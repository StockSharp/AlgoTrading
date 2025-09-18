# Cloudzs Trade 2 Strategy

## Overview
The **Cloudzs Trade 2 Strategy** is a StockSharp port of the MetaTrader 4 expert advisor `cloudzs_trade_2`. The original robot combines stochastic oscillator reversals with a double-fractal confirmation filter and uses aggressive trailing logic to protect open positions. This C# version recreates the signal flow and the trade management rules while exposing the parameters as `StrategyParam` objects so they can be optimised or adjusted from the StockSharp UI.

The strategy watches a single candle series (configurable timeframe) and evaluates two independent conditions:

1. **Stochastic reversal** – triggers when the %D line leaves an extreme zone (>= 80 for sells, <= 20 for buys) while confirming that %D crossed the %K line on the previous candle, closely matching the original MQL logic.
2. **Double fractal confirmation** – waits until two consecutive fractal signals of the same type appear (two upper fractals for sells or two lower fractals for buys).

If either condition generates a buy or sell request, the strategy enters in that direction (provided no trade is active and the previous exit was on a different day). When already in a trade, the same conditions can be used to exit early if `CloseOnOpposite` is enabled.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `LotSplitter` | Coefficient used to approximate trade volume from the current account value. | `0.1` |
| `MaxVolume` | Upper bound for the calculated volume (0 disables the cap). | `0` |
| `TakeProfitOffset` | Fixed take-profit distance in absolute price units. | `0` |
| `TrailingStopOffset` | Trailing stop distance in price units. | `0.01` |
| `StopLossOffset` | Fixed stop-loss distance in price units. | `0.05` |
| `MinProfitOffset` | Minimum profit to keep after a favourable excursion once `ProfitPointsOffset` was reached. | `0` |
| `ProfitPointsOffset` | Required favourable move before `MinProfitOffset` is enforced. | `0` |
| `%K Period` / `%D Period` / `Slowing` | Stochastic oscillator configuration. | `8 / 8 / 4` |
| `Method` | Original MT4 stochastic method identifier (informational, not used because StockSharp exposes a single implementation). | `3` |
| `PriceMode` | Original MT4 price mode identifier (informational only). | `1` |
| `UseStochasticCondition` | Enable stochastic based signal generation. | `true` |
| `UseFractalCondition` | Enable fractal based signal generation. | `true` |
| `CloseOnOpposite` | Close the active position when the opposite signal appears. | `true` |
| `CandleType` | Timeframe/data type used for calculations. | `15-minute time frame` |

## Trading Signals
### Long Entry
- %D line is below or equal to 20 and crosses below %K (matching the prior-candle comparison from MT4).
- **OR** two sequential lower fractals are detected.
- No open position and the last exit happened on a different calendar day.

### Short Entry
- %D line is above or equal to 80 and crosses above %K.
- **OR** two sequential upper fractals appear.
- No open position and the last exit happened on a different calendar day.

### Exit Rules
- Hard stop-loss or take-profit levels are reached (if configured).
- Trailing stop moves in the trade's favour and price touches the updated stop level.
- After the position experiences `ProfitPointsOffset` favourable movement, a pullback to `MinProfitOffset` closes the trade.
- Optional early reversal: if `CloseOnOpposite` is true and the opposite signal fires, the trade is closed.

## Risk Management
- Stop-loss and take-profit distances mimic the raw pip offsets from the MT4 code (interpreted here as price differences).
- Trailing stops are updated using the close price and only move in the profitable direction.
- The `LotSplitter` parameter tries to follow the original volume formula, scaling the trade size by account value and limiting it with `MaxVolume`.

## Notes and Limitations
- The StockSharp `StochasticOscillator` exposes a single smoothing implementation; therefore the `Method` and `PriceMode` parameters are kept for reference but do not change the indicator behaviour.
- The original MT4 script worked tick-by-tick. This port evaluates signals on finished candles to align with StockSharp best practices.
- Volume calculation relies on available portfolio values; if no account information exists, it falls back to the `LotSplitter` value.

## Usage
1. Add the strategy to your StockSharp project and select the instrument you want to trade.
2. Configure the candle timeframe and adjust the stochastic/fractal settings if needed.
3. Provide realistic stop-loss/take-profit offsets that match the instrument's tick size.
4. Start the strategy in Designer, Runner, or via API and monitor the log messages for signal information.
