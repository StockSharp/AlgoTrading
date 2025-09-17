# KA-Gold Bot Strategy

The **KA-Gold Bot Strategy** is a high-level StockSharp conversion of the original MetaTrader 4 "KA-Gold Bot" expert advisor. It combines a Keltner-style channel with trend filters and aggressive risk management that includes fixed stop-loss, take-profit, and multi-stage trailing protection. Trading is allowed only during a configurable intraday window and new positions are blocked when the live spread exceeds a threshold.

## Trading Logic

1. **Indicator preparation**
   - An exponential moving average (EMA) with length `KeltnerPeriod` builds the channel midline.
   - A simple moving average of candle ranges (high minus low) with the same period estimates the channel half-width.
   - Short-term and long-term exponential moving averages (`EmaShortPeriod` and `EmaLongPeriod`) track fast momentum and the higher-timeframe trend respectively.
   - All indicator values are recorded for the two most recent completed candles to mirror the MT4 shift-based calculations.

2. **Entry conditions**
   - Calculations run only when the current candle closes and the strategy is connected to the market with trading permissions granted.
   - The channel upper and lower bands are derived by adding/subtracting the averaged range from the EMA midline for both the previous (`shift = 1`) and the earlier (`shift = 2`) candle.
   - **Long setup:**
     - The previous close breaks above the most recent upper band.
     - The same close is above the long EMA, confirming an uptrend.
     - The short EMA crosses from below the older upper band to above the latest one (`EMA_short[2] < Upper[2]` and `EMA_short[1] > Upper[1]`).
   - **Short setup:**
     - The previous close falls below the recent lower band.
     - The same close is below the long EMA, confirming a downtrend.
     - The short EMA crosses from above the older lower band to below the latest one (`EMA_short[2] > Lower[2]` and `EMA_short[1] < Lower[1]`).
   - Only one position is allowed at a time. If a trade is already open, the signal is ignored.

3. **Timing and spread filters**
   - When `UseTimeFilter` is enabled, new entries are restricted to the `[StartHour:StartMinute, EndHour:EndMinute)` window using the exchange-local time. Overnight sessions are supported if the end time is earlier than the start time.
   - Level-1 quote subscriptions keep track of the best bid/ask prices. Before placing an order, the strategy converts the current spread into instrument points and compares it against `MaxSpreadPoints`. Orders are skipped, with logging, whenever the threshold is breached.

4. **Risk management**
   - Position sizing defaults to `FixedVolume`. If `UseRiskPercent` is `true`, the trade size is recalculated from the portfolio equity as `RiskPercent% / (riskPips * PipValue)`, where `riskPips` equals `StopLossPips` (fallback to `TrailingStopPips` when no fixed stop is defined). The final result is normalized to the instrument volume step and clamped between the minimum and maximum exchange limits.
   - When a long position is opened, the strategy stores:
     - Initial stop-loss at `entry - StopLossPips * pipSize` (if defined).
     - Initial take-profit at `entry + TakeProfitPips * pipSize` (if defined).
     - Trailing state flags, which reset the short-side trackers.
   - Short trades mirror the same logic with inverted price directions.

5. **Trailing protection**
   - Live bid/ask updates feed two trailing engines:
     - Once the floating profit exceeds `TrailingTriggerPips`, trailing becomes active.
     - The trailing stop is positioned `TrailingStopPips` away from the current favourable price and is only advanced when the move exceeds `TrailingStopPips + TrailingStepPips` beyond the previous stop level.
     - For long positions the trailing stop never drops below the original protective stop, and for shorts it never rises above it.
   - Exit monitoring is performed both on incoming quotes and on finished candles:
     - A position is closed immediately when price reaches the active stop (original or trailing).
     - Profits are also locked once the candle’s high/low touches the stored take-profit level.
   - After closing a position the protection state is fully reset to avoid stale data.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Data type describing the execution timeframe. | 1-minute time frame |
| `KeltnerPeriod` | Period for the EMA midline and the range average of the channel. | 50 |
| `EmaShortPeriod` | Fast EMA length used for crossover confirmation. | 10 |
| `EmaLongPeriod` | Slow EMA length acting as trend filter. | 200 |
| `FixedVolume` | Fallback order volume when percentage sizing is disabled. | 1 |
| `UseRiskPercent` | Enable percentage-based position sizing. | `true` |
| `RiskPercent` | Percentage of equity risked per trade. | 1 |
| `StopLossPips` | Distance of the fixed stop-loss in pips (0 disables). | 500 |
| `TakeProfitPips` | Distance of the fixed take-profit in pips (0 disables). | 500 |
| `TrailingTriggerPips` | Profit in pips required to activate the trailing stop. | 300 |
| `TrailingStopPips` | Distance between price and trailing stop once active. | 300 |
| `TrailingStepPips` | Minimum additional profit (in pips) before the trailing stop is advanced. | 100 |
| `UseTimeFilter` | Toggle for the trading session filter. | `true` |
| `StartHour` / `StartMinute` | Session start in exchange-local time. | 02:30 |
| `EndHour` / `EndMinute` | Session end in exchange-local time. | 21:00 |
| `MaxSpreadPoints` | Maximum allowed spread in instrument points (0 disables the check). | 65 |
| `PipValue` | Monetary value of one pip, used for risk-based position sizing. | 1 |

## Additional Notes

- Pip conversion follows the exchange instrument decimals: a five-digit quote (odd number of decimals) multiplies the price step by 10 to emulate the MT4 pip size logic.
- The strategy subscribes to both candles and level-1 data but does **not** register additional indicators on the chart, complying with the high-level API guidelines.
- Protective exits rely on market orders issued by the strategy; no separate stop or limit orders are placed on the exchange.
- Python support is not included in this delivery, matching the original request.
