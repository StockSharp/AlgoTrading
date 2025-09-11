# MultiLayer Awesome Oscillator Saucer

Implements a bullish multi-layer strategy based on the Awesome Oscillator saucer pattern and fractal trend detection. The strategy counts consecutive saucer signals and places up to five layered buy stop orders above price. Positions are closed when the trend reverses.

## Parameters
- **EMA Length** – period of the EMA filter.
- **Candle Type** – type of candles.
- **Trade Start** – start of trading period.
- **Trade Stop** – end of trading period.
