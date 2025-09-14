# Millenium Code Strategy

The **Millenium Code** strategy is a positional system that opens at most one trade per day. Direction is determined by a moving average crossover filtered by recent highs and lows. Trades are placed at a user-defined time and are closed by time, stop loss, take profit or maximum duration.

## Trading Logic

1. At the specified opening time the strategy checks if trading is allowed for the current day of week.
2. Fast and slow simple moving averages are compared. If the fast MA crosses above the slow MA and price confirms the breakout, a long position is opened. The opposite conditions open a short position.
3. Only one trade per day is allowed. Subsequent signals are ignored until the next trading day.
4. Positions are closed when:
   - Stop loss or take profit level is reached.
   - The configured closing time occurs.
   - The maximum trade duration is exceeded.

## Parameters

- **Candle Type** – timeframe of input candles.
- **Fast MA** – period of the fast moving average.
- **Slow MA** – period of the slow moving average.
- **HighLow Bars** – number of candles used to search recent highs and lows.
- **Reverse** – invert buy/sell signals.
- **Stop Loss** – distance to stop loss in price steps.
- **Take Profit** – distance to take profit in price steps.
- **Open Hour/Minute** – time to start looking for entries (-1 disables).
- **Close Hour/Minute** – time to close positions (-1 disables).
- **Duration** – maximum trade life in hours (0 disables).
- **Sunday ... Friday** – enable trading for each weekday.

## Notes

This strategy uses only high level API features and avoids accessing indicator history directly. It is intended as an educational example and not as investment advice.

