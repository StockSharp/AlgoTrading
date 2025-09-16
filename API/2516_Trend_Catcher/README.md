# Trend Catcher Strategy

## Overview
The Trend Catcher strategy is a conversion of the MetaTrader 5 expert advisor "Trend_Catcher_v2". It combines three exponential moving averages with the Parabolic SAR indicator to identify trend reversals and trend continuation opportunities. The system operates on a single symbol and timeframe and relies on end-of-candle calculations, which makes it suitable for backtesting in StockSharp Designer as well as for live execution through StockSharp API-based runners.

## Indicators and Filters
- **Parabolic SAR** — detects bullish and bearish flips that indicate potential reversals.
- **Slow EMA** — the higher timeframe trend filter that defines the dominant direction.
- **Fast EMA** — reacts faster to price changes to confirm the direction of the current swing.
- **Trigger EMA** — keeps the entry close to price action and avoids trades taken too far away from the mean.
- **Trading day switches** — optional filters to disable trading on selected weekdays.

## Trading Logic
### Long entries
1. The close price finishes above the current Parabolic SAR value.
2. The previous candle closed below the previous Parabolic SAR value (bullish flip).
3. The fast EMA is above the slow EMA, confirming an uptrend.
4. The close price is above the trigger EMA to avoid counter-trend signals.
5. No position is open and no position was closed during the current candle.

### Short entries
All the above conditions are mirrored:
1. The close price finishes below the current Parabolic SAR value.
2. The previous candle closed above the previous Parabolic SAR value (bearish flip).
3. The fast EMA is below the slow EMA.
4. The close price is below the trigger EMA.
5. No position is open and no position was closed during the current candle.

When the **Reverse Signals** switch is enabled the long and short conditions are inverted, allowing the strategy to trade breakouts in the opposite direction.

## Position Management
- **Automatic stop-loss** – when enabled the stop is calculated from the distance between price and Parabolic SAR multiplied by the `StopLossCoefficient`. The distance is clamped between `MinStopLoss` and `MaxStopLoss`.
- **Automatic take-profit** – multiplies the stop distance by `TakeProfitCoefficient`. Manual distances can be used when automation is disabled.
- **Risk-based position sizing** – the trade size is derived from portfolio equity and `RiskPercent`. When the most recent closed trade is a loss and **Use Martingale** is enabled the calculated size is multiplied by `MartingaleMultiplier`.
- **Breakeven and trailing stop** – after reaching `BreakevenTrigger` profit the stop is moved to the entry price plus `BreakevenOffset` (or minus for short trades). Once the position gains `TrailingTrigger`, the stop trails price by `TrailingStep`.
- **Close on opposite signal** – when active, the strategy exits an existing position as soon as an opposing setup appears.
- **One trade per candle** – the algorithm stores the timestamp of the latest exit and skips entries until the next candle opens.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Main timeframe used for all indicators. | 15-minute time frame |
| `CloseOnOppositeSignal` | Exit immediately when the reverse setup is detected. | `true` |
| `ReverseSignals` | Swap long and short conditions. | `false` |
| `TradeMonday` … `TradeFriday` | Enable or disable trading on specific weekdays. | `true` |
| `SlowMaPeriod` | Period of the slow EMA trend filter. | `200` |
| `FastMaPeriod` | Period of the fast EMA confirmation. | `50` |
| `FastFilterPeriod` | Period of the trigger EMA. | `25` |
| `SarStep` | Parabolic SAR acceleration step. | `0.004` |
| `SarMax` | Maximum Parabolic SAR acceleration. | `0.2` |
| `AutoStopLoss` | Enable dynamic stop-loss calculation. | `true` |
| `AutoTakeProfit` | Enable dynamic take-profit calculation. | `true` |
| `MinStopLoss` / `MaxStopLoss` | Lower and upper bounds for the stop distance. | `0.001` / `0.2` |
| `StopLossCoefficient` | Multiplier applied to the SAR distance. | `1` |
| `TakeProfitCoefficient` | Multiplier used for the take-profit distance. | `1` |
| `ManualStopLoss` | Fixed stop distance when automation is disabled. | `0.002` |
| `ManualTakeProfit` | Fixed target distance when automation is disabled. | `0.02` |
| `RiskPercent` | Percentage of portfolio equity risked per trade. | `2` |
| `UseMartingale` | Increase size after a losing trade. | `true` |
| `MartingaleMultiplier` | Multiplier applied after a loss. | `2` |
| `BreakevenTrigger` | Profit needed before moving the stop to breakeven. | `0.005` |
| `BreakevenOffset` | Buffer added when the stop is moved to breakeven. | `0.0001` |
| `TrailingTrigger` | Profit required to start trailing the stop. | `0.005` |
| `TrailingStep` | Distance maintained by the trailing stop. | `0.001` |

## Usage Notes
- The strategy sends market orders for both entries and exits; slippage controls should be added at the brokerage adapter level if required.
- Because the logic uses end-of-candle data, the accuracy of backtests depends on the granularity of the candle series supplied to the strategy.
- Parameters are fully exposed through `StrategyParam` objects, making them available for optimization in StockSharp Designer.
