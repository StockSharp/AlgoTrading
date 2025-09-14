# Zonal Trading Strategy

This strategy uses the Awesome Oscillator (AO) and the Accelerator Oscillator (AC) to capture changes in market momentum.

## Logic
- Buy when both AO and AC rise above their previous values and at least one of them has turned upward from the prior bar while both oscillators are positive.
- Sell when both AO and AC fall below their previous values and at least one of them has turned downward from the prior bar while both oscillators are negative.
- Close a long position when AO and AC turn downward.
- Close a short position when AO and AC turn upward.

## Parameters
- **Candle Type** – source candle series for calculations.
- **Take Profit** – fixed take profit value in price units.

The strategy trades a single position at a time using market orders.
