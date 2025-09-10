# Arpit Bollinger Band Strategy

Bollinger Band breakout strategy that waits for a close outside the bands two candles ago and enters when price breaks the extreme of that bar.

- **Indicators**: Bollinger Bands (EMA 20, deviation 1.5)
- **Entry**: Long when price closed below the lower band two bars ago and current high exceeds that bar's high. Short when price closed above the upper band two bars ago and current low falls below that bar's low.
- **Stops**: Stop placed beyond the current candle range with a 5% buffer and take profit based on a risk‑reward ratio.

