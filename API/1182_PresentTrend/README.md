# PresentTrend Strategy

Uses ATR-based thresholds with RSI or MFI to track trend direction. The PresentTrend line is built by expanding or contracting based on oscillator value and ATR. Signals appear when PresentTrend crosses its value from two bars ago and the most recent opposite signal confirms direction.

- **Long**: PresentTrend crosses above its value two bars earlier and the last short signal was more recent than the previous long.
- **Short**: PresentTrend crosses below its value two bars earlier and the last long signal was more recent than the previous short.
- **Indicators**: ATR, RSI or MFI.
- **Stops**: Closes position when opposite signal appears in one-sided mode.
