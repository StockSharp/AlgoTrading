# Smart Ass Trade Strategy

Smart Ass Trade is a multi-timeframe trend following strategy converted from the MQL implementation.
It analyzes MACD histogram (OsMA) and 20-period simple moving averages on 5, 15 and 30 minute charts.
A daily Williams %R filter blocks trades in overbought or oversold conditions.

## Algorithm
1. Calculate MACD histogram and SMA(20) on 5m, 15m and 30m timeframes.
2. Define uptrend when histogram grows and SMA rises on all three timeframes.
3. Define downtrend when histogram declines and SMA falls on all three timeframes.
4. Use daily Williams %R (period 26) to avoid buying above -2 or selling below -98.
5. When all conditions align open a market order in the corresponding direction.
6. Position size can be fixed or optimized from account value.

## Parameters
- **Hedging** – allow opening opposite positions.
- **LotsOptimization** – enable dynamic lot sizing.
- **Lots** – fixed trading volume when optimization is off.
- **AutomaticTakeProfit** – placeholder for dynamic take profit, currently unused.
- **MinimumTakeProfit** – profit target in points for manual mode.
- **AutomaticStopLoss** – placeholder for dynamic stop loss, currently unused.
- **StopLoss** – stop loss in points for manual mode.
- **CandleType** – base timeframe for subscriptions (default 5 minutes).

## Notes
The strategy uses the high level API with `SubscribeCandles` and `Bind` calls.
Take profit and stop loss values are left for further extension; current version focuses on
signal generation and order execution.
