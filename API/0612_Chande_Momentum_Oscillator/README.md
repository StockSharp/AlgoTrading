# Chande Momentum Oscillator Strategy

Uses the Chande Momentum Oscillator and its EMA signal line. Enters short when the signal is above the overbought threshold and the histogram decreases above zero. Enters long when the signal is below the oversold threshold and the histogram increases below zero. A cooldown period prevents frequent signals. Positions are closed when the signal crosses the opposite threshold.

## Parameters
- Candle Type
- CMO length
- Signal length
- Cooldown bars
- Overbought threshold
- Oversold threshold
- Allow long
- Allow short
