# Ma Shift Puria Method Strategy

## Overview
The Ma Shift Puria Method strategy is an implementation of the classic “Puria” Expert Advisor adapted for StockSharp's high-level API. The algorithm combines multiple exponential moving averages (EMAs) with a MACD filter and optional fractal-based trailing logic. Signals are evaluated on completed candles only. Position management includes fixed stop-loss and take-profit levels, configurable trailing stops, and an optional fractal trailing mode that locks in profits near the target when a confirmed swing point appears.

## Indicators and Calculations
- **Fast EMA (default 14)** – captures short-term momentum and defines the slope of the fast average.
- **Slow EMA (default 80)** – represents broader market direction. The distance between the fast and slow EMAs must exceed a user-defined pip threshold to validate signals.
- **MACD (fast 11, slow 102, signal 9)** – confirms directional momentum by requiring the main line to cross the zero axis in the direction of the trade while having been on the opposite side three bars earlier.
- **Fractal window (5 bars)** – used when fractal trailing is enabled. The strategy derives swing highs and lows from a rolling five-bar buffer, matching the MetaTrader fractal definition (center bar is the local extreme compared to two bars on each side).

## Entry Logic
A new position is opened only when the strategy is allowed to trade and the following conditions are true on the most recent completed candle:

### Long Entry
1. Fast EMA is above the slow EMA.
2. Slow EMA is trending upward compared to its value three bars ago.
3. Fast EMA has an upward slope (current value above the previous value).
4. MACD main line is above zero and was below zero three bars ago.
5. The fast EMA increased by more than the configured **Shift Minimum** (in pips) between the last two bars, and either keeps accelerating or the previous increment was non-positive.

### Short Entry
1. Fast EMA is below the slow EMA.
2. Slow EMA is trending downward compared to three bars ago.
3. Fast EMA has a downward slope (current value below the previous value).
4. MACD main line is below zero and was above zero three bars ago.
5. The fast EMA decreased by more than the **Shift Minimum** threshold and either continues to accelerate or the prior increment was non-negative.

The strategy opens positions in fixed increments (manual volume) or dynamically sized units based on portfolio risk, depending on the chosen mode. When an opposite position is open, the algorithm closes it and opens a new one in the current direction in a single market order.

## Exit and Risk Management
- **Stop Loss** – set in pips relative to the entry price. If the candle's low/high touches the protective level, the position is closed immediately.
- **Take Profit** – also expressed in pips. Hitting the target closes the entire position.
- **Trailing Stop** – when enabled, the stop level trails price by the configured distance after profits exceed the trailing distance plus trailing step. The logic mirrors the original MQL expert, updating only when the stop can move by at least the trailing step.
- **Fractal Trailing** – optional. Once price covers 95% of the take-profit distance, the stop can be moved to the latest swing low (long) or swing high (short) identified by the five-bar fractal pattern, tightening risk while leaving room for a breakout.
- **Risk-Based Sizing** – if manual volume is disabled, the strategy risks a fixed percentage of the portfolio per trade. It divides the capital at risk by the monetary stop distance and rounds the result to the closest allowed volume step within exchange limits.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `UseManualVolume` | Toggle between fixed volume and risk-based sizing. | `true` |
| `ManualVolume` | Volume used per trade when manual sizing is active. | `0.1` |
| `RiskPercent` | Percent of equity risked per trade (used when `UseManualVolume` is false). | `9` |
| `StopLossPips` | Stop-loss distance in pips. | `45` |
| `TakeProfitPips` | Take-profit distance in pips. | `75` |
| `TrailingStopPips` | Trailing stop distance in pips. | `15` |
| `TrailingStepPips` | Minimum pip move before updating the trailing stop. | `5` |
| `MaxPositions` | Maximum number of position units that can be accumulated in one direction. | `1` |
| `ShiftMinPips` | Minimum EMA slope in pips required for a valid signal. | `20` |
| `FastLength` | Fast EMA length. | `14` |
| `SlowLength` | Slow EMA length. | `80` |
| `MacdFast` | MACD fast period. | `11` |
| `MacdSlow` | MACD slow period. | `102` |
| `UseFractalTrailing` | Enable/disable fractal trailing stop adjustments. | `false` |
| `CandleType` | Candle type (time frame) used for calculations. | `15-minute` |

## Implementation Notes
- The strategy subscribes to one candle stream and binds EMA and MACD indicators via `SubscribeCandles().Bind(...)`, ensuring indicator values are received in the signal handler without manual buffer queries.
- Internal state tracks the last three EMA and MACD values to mimic the MQL `shift` indexing required by the original logic.
- Fractals are computed locally using a five-bar rolling window, matching MetaTrader's behavior without calling `GetValue` on the indicator.
- Stop and take-profit management is performed with market exits when price levels are breached, mirroring the effect of the original position modifications.
- The `StartProtection()` call enables built-in StockSharp position monitoring for resilience during unexpected disconnects.

## Usage Recommendations
1. Select an appropriate candle type (e.g., 15-minute bars for major FX pairs) to reflect the original Puria setup.
2. Adjust the pip-based parameters to match the instrument's point value. The helper automatically scales to five-digit quotes, but exotic instruments might require custom tuning.
3. When enabling risk-based sizing, verify the portfolio valuation and volume step constraints to ensure the calculated volume is tradable.
4. Combine with portfolio-level money management or session filters if needed; the strategy focuses strictly on signal and trailing logic from the original MQL expert.
