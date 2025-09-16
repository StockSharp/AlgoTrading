# ASCV BrainTrend Signal Strategy

The **ASCV BrainTrend Signal Strategy** is a conversion of the MetaTrader expert that trades on BrainTrend1 indicator signals. The StockSharp version relies on high-level indicator bindings to combine the Average True Range (ATR), Stochastic Oscillator, and Jurik Moving Average (JMA) in order to detect momentum reversals and place trades with optional protective stops.

## Core Idea

1. Calculate ATR to measure current volatility and define a dynamic confirmation band.
2. Smooth closing prices with a Jurik Moving Average and compare the current value with the value two bars back.
3. When the smoothed difference is larger than `ATR / 2.3`, update the state of the BrainTrend logic:
   - `%K` of the Stochastic Oscillator below **47** toggles the system into a potential short setup.
   - `%K` above **53** toggles the system into a potential long setup.
4. A signal from the previous bar is executed on the next completed candle. Signals can be flipped with the **Reverse Signals** parameter.
5. Stop-loss, take-profit, and trailing-stop levels are defined in pips (multiples of the instrument price step).

## Entry and Exit Rules

- **Long entry**: Previous bar issued a buy signal and the strategy is not already long. The order size equals `Volume + abs(current position)`, so shorts are covered before opening the new long.
- **Short entry**: Previous bar issued a sell signal and the strategy is not already short.
- **Stop-loss**: Placed at `entry price ± StopLossPips * price step`. If the price trades beyond the stop level inside the next candle, the position is closed at market.
- **Take-profit**: Optional take profit at `entry price ± TakeProfitPips * price step`.
- **Trailing-stop**: Enabled when both `TrailingStopPips` and `TrailingStepPips` are greater than zero. After the price moves `TrailingStopPips + TrailingStepPips` in favor of the trade, the stop is trailed behind the move by `TrailingStopPips`.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `AtrPeriod` | ATR averaging period for volatility estimation. | 14 |
| `StochasticPeriod` | Base period for the Stochastic Oscillator. | 12 |
| `JmaLength` | Jurik Moving Average smoothing length. | 7 |
| `StopLossPips` | Stop-loss distance in pips (price steps). | 15 |
| `TakeProfitPips` | Take-profit distance in pips. | 46 |
| `TrailingStopPips` | Trailing stop distance in pips. | 0 (disabled) |
| `TrailingStepPips` | Minimum favorable move required before trailing. | 5 |
| `ReverseSignals` | Invert buy/sell signals. | false |
| `CandleType` | Working timeframe, defaults to 15-minute candles. | 15m |

## Notes

- All indicator calculations are performed on finished candles to avoid mid-bar noise.
- If the instrument does not supply `MinPriceStep`, a default step of `0.0001` is used when converting pip distances.
- The strategy draws candles, the Stochastic oscillator, and the JMA on the chart for monitoring.
- Trailing stops mirror the original MetaTrader logic: they only move in the direction of the trade and require both distance and step thresholds to be met.

## Usage Tips

- Adjust `AtrPeriod` and `StochasticPeriod` to fit the volatility of the instrument being traded.
- Increase the pip-based risk parameters when trading assets with larger tick sizes (e.g., futures) to avoid immediate stop-outs.
- Enable `ReverseSignals` to mirror the original Expert Advisor's reverse mode.
- Combine with broker-side risk controls if real-money trading is involved.
