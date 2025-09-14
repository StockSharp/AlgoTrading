# Color Schaff JCCX Trend Cycle Strategy

This strategy is a C# conversion of the MQL5 expert `Exp_ColorSchaffJCCXTrendCycle`.
It employs the **Schaff Trend Cycle (STC)** oscillator built on top of the JCCX algorithm.

## Trading Logic

* Calculate the Schaff Trend Cycle on each finished candle.
* When the oscillator drops below the `High Level` after being above it, a long position is opened and short positions are closed.
* When the oscillator rises above the `Low Level` after being below it, a short position is opened and long positions are closed.

## Parameters

| Name | Description |
|------|-------------|
| Fast JCCX | Fast JCCX period used in the indicator. |
| Slow JCCX | Slow JCCX period used in the indicator. |
| Smoothing | JJMA smoothing factor for JCCX. |
| Phase | JJMA phase value. |
| Cycle | Length of the cycle for Schaff Trend calculation. |
| High Level | Upper trigger level of the oscillator. |
| Low Level | Lower trigger level of the oscillator. |
| Open Long | Allow opening long positions. |
| Open Short | Allow opening short positions. |
| Close Long | Allow closing existing long positions. |
| Close Short | Allow closing existing short positions. |

## Notes

The strategy uses StockSharp's high level API and subscribes to candle data. It reacts only to **finished** candles. Money management and risk control are kept simple for demonstration purposes.
