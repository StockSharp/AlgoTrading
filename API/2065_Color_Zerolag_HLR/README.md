# Color Zerolag HLR Strategy

This strategy is a C# conversion of the MQL5 expert `Exp_ColorZerolagHLR`. It combines multiple Hi-Lo Range (HLR) oscillators with different lengths and weights, then applies an exponential smoothing to build fast and slow trend lines. Crossovers between these lines generate trading signals.

## Overview
- Builds five HLR values using highest high and lowest low over specified periods.
- Weights each HLR and sums them to produce a fast trend line.
- Applies zero-lag smoothing to derive a slow trend line.
- Trades when the fast line crosses the slow line.

## Parameters
- `Smoothing` – EMA smoothing factor.
- `Factor1`..`Factor5` – weights for each HLR component.
- `HlrPeriod1`..`HlrPeriod5` – lookback periods for HLR calculations.
- `BuyPosOpen`/`SellPosOpen` – allow opening long or short positions.
- `BuyPosClose`/`SellPosClose` – allow closing existing positions.
- `CandleType` – timeframe of candles.

## Indicators
- Highest, Lowest (five pairs each).

## Trading Logic
- If the previous fast line was above the slow line and now crosses below, the strategy opens a long position and closes any short.
- If the previous fast line was below the slow line and now crosses above, the strategy opens a short position and closes any long.

Use this template as a starting point and adjust parameters or risk management according to your needs.
