# Psar Bug 6 Strategy

Converted from MQL4 script "psar_bug_6".

## Logic
- Uses Parabolic SAR indicator with configurable step and maximum acceleration.
- Goes long when price closes above SAR and previously was below.
- Goes short when price closes below SAR and previously was above.
- Optional reversal parameter flips buy/sell signals.
- Option `SarClose` closes existing position when SAR flips to opposite side.
- Fixed take profit and stop loss distances in price units. Trailing stop can be enabled.

## Parameters
- `SarStep` – acceleration factor step.
- `SarMax` – maximum acceleration factor.
- `StopLoss` – initial stop loss distance.
- `TakeProfit` – take profit distance.
- `Trailing` – enable trailing stop.
- `TrailStop` – trailing stop distance when trailing is enabled.
- `SarClose` – close position on SAR reversal.
- `Reverse` – invert trading signals.
- `CandleType` – candle type for calculations.

## Notes
The strategy uses high level API with candle subscriptions and indicator binding. Protection is started with optional trailing stop and market order exits.
