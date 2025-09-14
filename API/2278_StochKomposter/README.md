# Stoch Komposter Strategy

This strategy is a port of the MQL5 expert **Exp_iStochKomposter**. It uses the Stochastic Oscillator to detect momentum reversals and trades when the %K line crosses predefined thresholds.

## How It Works

- Calculates the Stochastic Oscillator on the selected timeframe.
- Generates a **buy** signal when %K crosses above the lower level (default 30).
- Generates a **sell** signal when %K crosses below the upper level (default 70).
- On each signal the strategy closes any opposite position and opens a new position in the signal direction using market orders.
- Optional stop loss and take profit levels are applied via `StartProtection`.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `KPeriod` | Calculation period of the %K line | 5 |
| `DPeriod` | Smoothing period of the %D line | 3 |
| `UpLevel` | Overbought threshold to trigger sells | 70 |
| `DownLevel` | Oversold threshold to trigger buys | 30 |
| `StopLoss` | Absolute stop loss in price units | 1000 |
| `TakeProfit` | Absolute take profit in price units | 2000 |
| `CandleType` | Timeframe for calculations | 1 hour |

## Notes

- The strategy operates only on finished candles.
- It does not calculate ATR levels from the original indicator; those were used only for arrow placement in the MQL version.
- Position size is defined by the `Volume` property of the strategy.
