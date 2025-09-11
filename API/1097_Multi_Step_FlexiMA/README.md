# Multi-Step FlexiMA Strategy

Uses a variable-length moving average oscillator with a SuperTrend filter and multi-step take profit.

- **Long** when price is above the SuperTrend line and the oscillator is positive.
- **Short** when price is below the SuperTrend line and the oscillator is negative.
- **Partial exits** at three take-profit levels.
- **Close** remaining position when the opposite condition appears.

**Indicators**: Variable-length SMA oscillator, SuperTrend
**Stops**: multi-step take profit only
