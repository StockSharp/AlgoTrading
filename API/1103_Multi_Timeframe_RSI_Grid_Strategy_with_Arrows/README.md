# Multi-Timeframe RSI Grid Strategy with Arrows

This strategy trades when RSI on the current timeframe and two higher timeframes reach overbought or oversold levels. The first position is opened when all RSIs align, then additional positions are added using an ATR based grid with an increasing lot multiplier. The strategy targets a daily profit percentage, resets each day, and closes on reverse signals or drawdown.

## Parameters
- Candle Type
- RSI Length
- Oversold Level
- Overbought Level
- Higher Timeframe 1
- Higher Timeframe 2
- Grid Multiplication Factor
- Lot Multiplication Factor
- Maximum Grid Levels
- Daily Profit Target %
- ATR Length
