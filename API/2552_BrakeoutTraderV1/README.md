# Brakeout Trader v1
[Русский](README_ru.md) | [中文](README_cn.md)

Brakeout Trader v1 is a simple breakout system built around a static price level. The strategy watches the closing prices of completed candles and enters when the market closes through the chosen breakout level. When the close crosses above the level, a long position is opened (subject to direction filters); when it crosses below, a short position is taken. Position size is calculated from the configured risk percentage and the distance to the stop-loss, enabling automatic scaling with the account equity.

## Trading Logic
- Process finished candles from the selected `CandleType` only. Unfinished candles are ignored.
- Keep the last closed price to detect breakouts of the user-specified `BreakoutLevel`.
- **Long entry**: the latest candle closes above `BreakoutLevel` while the previous close was at or below the level, and `EnableLong` is true. Any open short is flattened before the new order is submitted.
- **Short entry**: the latest candle closes below `BreakoutLevel` while the previous close was at or above the level, and `EnableShort` is true. Any open long is closed first.
- Orders are sent at market. The quantity is computed so that the loss between the entry price and the stop-loss distance corresponds to `RiskPercent` of the current account equity. If the risk-based size cannot be determined, the strategy falls back to the base `Volume` value.
- After each entry the strategy stores static profit-taking and stop-loss levels expressed in pip points (`StopLossPoints` and `TakeProfitPoints`). When price reaches either level, the open position is closed at market and the cached levels are cleared.
- Multiple trades are never open in the same direction simultaneously because the net position is managed explicitly.

## Position Management
- A protective stop is set below the entry for long trades and above the entry for shorts. The distance is `StopLossPoints * pip`, where pip is derived from `Security.PriceStep` and its precision (3 or 5 decimal places imply a tenfold adjustment, as in the original MQL implementation).
- A profit target is set symmetrically using `TakeProfitPoints`.
- If both stop and target would trigger during the same candle, the stop check is evaluated first, mirroring conservative server-side execution.
- Opposite signals always close any active position before establishing the new one, preventing hedged exposure.
- The helper automatically resets cached levels whenever the position returns to zero.

## Parameters
- `BreakoutLevel` – Static price level monitored for breakouts.
- `EnableLong` / `EnableShort` – Directional filters that allow opening longs or shorts.
- `StopLossPoints` – Stop-loss distance in pip points (multiples of the derived pip size).
- `TakeProfitPoints` – Take-profit distance in pip points.
- `RiskPercent` – Percentage of account equity to risk per trade. Used to determine the order volume from the stop-loss distance.
- `CandleType` – Candle data series used for signal generation (default 15-minute candles).
- `Volume` – Base order size used when the risk-based calculation is not available.

## Details
- **Entry Criteria**: Close crosses above/below `BreakoutLevel` on the last completed candle.
- **Long/Short**: Trades both directions, controlled by the `EnableLong` and `EnableShort` flags.
- **Exit Criteria**: Static stop-loss and take-profit levels, plus flattening on opposite breakout signals.
- **Stops**: Fixed-distance stop-loss measured in pip points.
- **Default Values**: `BreakoutLevel = 0`, `StopLossPoints = 140`, `TakeProfitPoints = 180`, `RiskPercent = 10`, `CandleType = 15-minute`, `EnableLong = EnableShort = true`.
- **Filters**: None beyond the direction toggles; no trend or volatility filters are applied.

## Usage Notes
- The instrument should support the pip calculation used by the original EA. For symbols with 3 or 5 decimal places the pip is automatically scaled by ten.
- Ensure the connected portfolio provides `CurrentValue` so that risk-based sizing functions properly. If equity is unavailable, trades will default to the configured `Volume`.
- Because orders are executed at market, actual fills may differ from the candle close. Adjust stop and take distances to account for slippage if needed.
