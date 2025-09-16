# VininI Trend Strategy

## Description
This strategy converts the original MQL expert advisor **Exp_VininI_Trend** into StockSharp. It uses the Commodity Channel Index (CCI) to emulate the VininI Trend oscillator. A long position is opened when the oscillator breaks above the upper level or twists upward. A short position is opened when the oscillator drops below the lower level or twists downward. The strategy works on completed candles only.

## Parameters
- **CCI Period** – length for the CCI indicator.
- **Upper Level** – threshold that triggers buy signals.
- **Lower Level** – threshold that triggers sell signals.
- **Entry Mode** – `Breakdown` reacts to level crossings, `Twist` reacts to direction changes.
- **Candle Type** – timeframe of candles used for calculations.

## Original
Converted from the MQL5 strategy located at `MQL/1365/exp_vinini_trend.mq5`.
