# Crossing of Two iMA v2 Strategy

## Overview
This strategy recreates the MetaTrader "Crossing of two iMA v2" expert advisor using StockSharp's high-level API. Two shifted moving averages generate crossover signals, optionally filtered by a third moving average. Protective stops, fixed or percentage-based position sizing, and a bar-by-bar trailing stop emulate the behaviour of the original robot while keeping the implementation compliant with StockSharp best practices.

## Indicators and Inputs
- **First Moving Average** – configurable period, shift, smoothing method, and applied price.
- **Second Moving Average** – independent configuration with the same set of options.
- **Third Moving Average Filter** – optional trend filter that keeps long trades only when the first MA is above the filter and short trades when the first MA is below the filter.
- **Candle Type** – controls the timeframe/series delivered by the data subscription.

## Trade Logic
### Step 1 – Immediate crossover
1. On every finished candle the strategy updates all moving averages using the selected applied prices.
2. A **long** entry is triggered when the first MA crosses **above** the second MA between the previous and current bar.
3. A **short** entry is triggered when the first MA crosses **below** the second MA between the previous and current bar.
4. When the filter is enabled, long signals require the first MA to stay **below** the filter MA, while short signals require it to remain **above** the filter MA.

### Step 2 – Delayed confirmation
If no signal fires in Step 1, the strategy checks for a crossover that started two bars ago but is still valid. This mirrors the original EA behaviour that searches the recent history for missed crosses. To avoid repeated fills, the signal only activates when at least three bars passed since the last trade.

### Order execution
- Entries are executed at market price. Opposite positions are closed before opening in the new direction.
- Exits occur when either the stop loss, take profit, or trailing stop levels are touched on the current candle. The trade is closed with a market order once a protective level is violated.

## Risk Management
- **Stop Loss** and **Take Profit** distances are configured in pips. They are converted to price offsets using the instrument's `PriceStep` (defaulting to `1` when unavailable).
- **Trailing Stop** starts from the entry price and follows favourable price movement. The stop is updated whenever the best price advances by at least `TrailingStepPips` pips beyond the previous trailing level.
- If both a fixed stop and a trailing stop are active, the strategy uses the more conservative level (higher for long positions, lower for short positions).

## Position Sizing
- When `UseRiskPercent` is **true**, the volume equals `Equity * RiskPercent / (StopLossPips * PipValue)`. If no stop is defined, the strategy falls back to the fixed volume.
- When `UseRiskPercent` is **false**, the trade size is always `FixedVolume`.
- `PipValue` should reflect the monetary value of a single pip per one lot/contract of the traded instrument.

## Implementation Notes
- The StockSharp implementation works entirely on closed candles and does not register pending orders. Users who need stop or limit entries can extend the strategy accordingly.
- The third moving average filter can be disabled to trade every crossover, matching the EA's option `InpFilterMA = false`.
- Ensure the candle type, price step, and pip value parameters match the instrument being traded for correct risk control.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `FirstPeriod` | Period of the first moving average. | 5 |
| `FirstShift` | Shift (bars) applied to the first moving average output. | 3 |
| `FirstMethod` | Smoothing method of the first moving average (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Smoothed` |
| `FirstAppliedPrice` | Applied price for the first moving average (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `SecondPeriod` | Period of the second moving average. | 8 |
| `SecondShift` | Shift (bars) applied to the second moving average output. | 5 |
| `SecondMethod` | Smoothing method for the second moving average. | `Smoothed` |
| `SecondAppliedPrice` | Applied price for the second moving average. | `Close` |
| `UseFilter` | Enables the third moving average directional filter. | `true` |
| `ThirdPeriod` | Period of the third moving average filter. | 13 |
| `ThirdShift` | Shift (bars) applied to the third moving average output. | 8 |
| `ThirdMethod` | Smoothing method for the third moving average filter. | `Smoothed` |
| `ThirdAppliedPrice` | Applied price for the third moving average filter. | `Close` |
| `UseRiskPercent` | Toggle between fixed volume and percentage-based position sizing. | `true` |
| `FixedVolume` | Trade size when fixed sizing is active. | 0.1 |
| `RiskPercent` | Fraction of account equity risked per trade. | 5 |
| `PipValue` | Monetary value of one pip per lot/contract. | 1 |
| `StopLossPips` | Stop-loss distance in pips. | 50 |
| `TakeProfitPips` | Take-profit distance in pips. | 50 |
| `TrailingStopPips` | Trailing stop distance in pips. | 10 |
| `TrailingStepPips` | Minimum pip increment required to advance the trailing stop. | 4 |
| `CandleType` | Candle data type / timeframe used by the strategy. | 1-minute candles |

