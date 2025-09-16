# Take Profit Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the MetaTrader "take-profit" expert by looking for four consecutive candles with strictly monotonic highs and opens. When the current candle completes with rising highs and opens the algorithm treats the sequence as bullish momentum and submits a market buy. A mirrored condition with falling highs and opens produces a market sell. Orders are managed with an account-level profit target, a trailing stop that can partially close exposure, and an optional fixed stop-loss defined in price steps.

The default configuration trades on one-minute candles. The strategy can be tuned for different instruments by adjusting the candle type, shift indexes that control which candles are compared, trailing distance, stop-loss distance, profit target, and position sizing mode. It supports either a fixed lot size or a dynamic volume computed from the portfolio equity and user-defined risk percentage. When the trailing stop advances the algorithm can optionally close half of the remaining position to lock in profits while keeping a runner active.

Reaching the configured profit target measured on portfolio equity immediately liquidates the current position and cancels any working orders. This mirrors the original MQL expert that closed all trades when account equity exceeded balance plus the desired gain. The risk-management branch validates the configured risk percentage and ensures the requested volume respects the security volume step.

## Details

- **Entry Logic**:
  - **Long**: the four monitored candles show strictly increasing highs and strictly increasing opens.
  - **Short**: the four monitored candles show strictly decreasing highs and strictly decreasing opens.
- **Position Management**:
  - Optional stop-loss placed at the entry price minus/plus the configured number of price steps.
  - Trailing stop follows the close price once it moves more than the trailing distance from the entry.
  - Partial exit (50% of the remaining volume) is executed every time the trailing stop moves, subject to the security volume step and the minimum tradable lot.
- **Account Target**: closes all exposure and cancels active orders when `portfolio equity ≥ initial equity + ProfitTarget`.
- **Risk Management**:
  - Fixed lot mode uses the configured `Lots` parameter (or `Volume` from the strategy base if specified).
  - Risk-percent mode sizes the order as `equity * RiskPercent / max(stopDistance, price)` and normalizes the result by the volume step.
- **Default Parameters**:
  - `Shift1` = 0, `Shift2` = 1, `Shift3` = 2, `Shift4` = 3.
  - `TrailingStopPoints` = 1, `StopLossPoints` = 0, `ProfitTarget` = 1 (account currency units).
  - `Lots` = 1, `RiskPercent` = 1, `MaxOrders` = 1.
  - `CandleType` = 1-minute time frame.
- **Best Markets**: trending futures, FX majors, and liquid crypto pairs where short-term momentum persists across multiple candles.
- **Strengths**: fast momentum detection, configurable equity target, partial scaling-out, and simple risk controls.
- **Weaknesses**: sensitive to noisy ranges, depends on correct step sizes, and assumes netting mode (single aggregated position).

## Parameters

| Name | Description |
| --- | --- |
| `Shift1` – `Shift4` | Indexes of the candles compared for the breakout sequence. |
| `TrailingStopPoints` | Trailing distance in price steps. |
| `StopLossPoints` | Initial stop distance in price steps; zero disables the stop-loss. |
| `ProfitTarget` | Profit target applied to portfolio equity before closing all trades. |
| `Lots` | Fixed trading volume when risk management is disabled. |
| `RiskManagement` | Enables risk-based sizing using `RiskPercent`. |
| `RiskPercent` | Percentage of portfolio equity risked on each trade when risk management is active. |
| `PartialClose` | If enabled, closes half of the position whenever the trailing stop moves. |
| `MaxOrders` | Maximum number of base units allowed simultaneously (net position limit). |
| `CandleType` | Time frame used for signal generation. |

## Usage Tips

1. Align the `Shift` parameters with the instrument's volatility. Larger shifts analyze longer momentum sequences.
2. Set `TrailingStopPoints` relative to the security price step; too small values may generate rapid partial exits.
3. Use risk-percent sizing with an explicit `StopLossPoints` so the position size reflects the actual monetary risk per trade.
4. Monitor the equity curve: once the global target is hit the strategy stops trading until restarted, mimicking the original EA.
