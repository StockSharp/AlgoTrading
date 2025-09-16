# Expotest Strategy

## Overview
The Expotest Strategy is a direct StockSharp conversion of the original `Expotest.mq5` expert advisor. It trades a single instrument using the Parabolic SAR indicator and a simple martingale-inspired money management rule. The strategy opens only one position at a time and relies on predefined stop-loss and take-profit levels for exits.

## Trading Logic
- **Indicator**: Parabolic SAR calculated on the selected candle series. Both the acceleration factor (`SarStep`) and maximum acceleration (`SarMaximum`) are configurable.
- **Entry conditions**: When no position is open, the strategy checks the latest closed candle.
  - If the Parabolic SAR value is below or equal to the close price, a long position is initiated.
  - If the Parabolic SAR value is above or equal to the close price, a short position is initiated.
- **Exit conditions**: Stop-loss and take-profit levels are placed at a fixed distance from the entry price, measured in price steps. During every new candle the strategy monitors whether the candle range touches either level and closes the position accordingly. The exit type (profit or loss) is remembered for future position sizing decisions.

## Position Sizing
- **Base volume**: Defined by the `FixedVolume` parameter when it is greater than zero. Otherwise the strategy estimates size from the `RiskPercent` and `StopLossPoints` values using current portfolio equity. If neither method returns a valid size the default `Strategy.Volume` is used.
- **Martingale step**: After a losing trade the next position size is doubled compared to the volume of the losing position. A profitable exit resets the multiplier and the next order uses the base volume again.

## Configurable Parameters
- `CandleType` – Data type for candle aggregation (time-frame or other candle format).
- `SarStep` – Initial acceleration factor for the Parabolic SAR.
- `SarMaximum` – Maximum acceleration factor for the Parabolic SAR.
- `StopLossPoints` – Stop-loss distance from entry expressed in price steps.
- `TakeProfitPoints` – Take-profit distance from entry expressed in price steps.
- `RiskPercent` – Percentage of portfolio equity to risk per trade when dynamic sizing is enabled.
- `FixedVolume` – Explicit order volume. Set to `0` to enable risk-based sizing.

## Additional Notes
- The strategy processes only finished candles in order to stay close to the original tick-based MQL implementation while remaining compatible with StockSharp subscriptions.
- Protective levels are tracked internally instead of separate stop/limit orders, which keeps the logic transparent and easy to backtest.
- Python implementation is intentionally omitted as requested.
