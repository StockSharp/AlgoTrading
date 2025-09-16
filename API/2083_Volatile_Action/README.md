# Volatile Action Strategy

This strategy combines a short-term volatility breakout with Bill Williams' **Alligator** trend filter calculated on the 4-hour timeframe.

## Trading Rules
- **Long entry** when:
  - The 1-period ATR is greater than *Volatility Coef* times the ATR with period *ATR Period*.
  - The candle is bullish and sets a new 24-bar high.
  - Alligator lines are aligned up (Lips > Teeth > Jaw) and both the open and close are above the Teeth line.
- **Short entry** when the above conditions are mirrored in the opposite direction.

On entry the strategy sets stop-loss and take-profit levels at multiples of the 1-period ATR:
- Stop-loss = entry price ± *Stop Coef* × ATR(1)
- Take-profit = entry price ± *Profit Coef* × ATR(1)

## Parameters
- **Volatility Coef** – multiplier comparing fast ATR to slow ATR.
- **ATR Period** – period of the slow ATR.
- **Stop Coef** – ATR multiplier for stop-loss.
- **Profit Coef** – ATR multiplier for take-profit.
- **Candle Type** – timeframe for the main analysis (Alligator uses 4H candles).
