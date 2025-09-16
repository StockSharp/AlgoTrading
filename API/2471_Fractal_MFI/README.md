# Fractal MFI Strategy

This strategy is a translation of the `Exp_Fractal_MFI.mq5` expert advisor. It uses the Money Flow Index (MFI) indicator to generate trading signals when the oscillator crosses predefined upper and lower levels.

## How It Works
- Calculates MFI over a configurable period.
- When the previous MFI value was above the **Low Level** and the current value falls below it, a signal is generated.
  - In **Direct** mode this opens a long position and optionally closes shorts.
  - In **Against** mode this opens a short position and optionally closes longs.
- When the previous MFI value was below the **High Level** and the current value rises above it, another signal is generated.
  - In **Direct** mode this opens a short position and optionally closes longs.
  - In **Against** mode this opens a long position and optionally closes shorts.

Only completed candles are processed. The strategy can be configured to enable or disable opening and closing of long or short positions separately.

## Parameters
- `MfiPeriod` – period of the Money Flow Index calculation.
- `HighLevel` – upper threshold for the MFI.
- `LowLevel` – lower threshold for the MFI.
- `CandleType` – candle timeframe used in calculations.
- `Trend` – choose `Direct` to trade with the indicator direction or `Against` to invert signals.
- `BuyPosOpen` / `SellPosOpen` – allow opening long or short positions.
- `BuyPosClose` / `SellPosClose` – allow closing existing positions on opposite signals.

## Notes
This C# version focuses on high level API usage and does not implement the original money management rules or stop levels from the MQL code.
