# Trend Continuation Strategy

This strategy identifies continuation of the prevailing trend using a pair of exponential moving averages on price data. A long position is opened when the fast EMA crosses above the slow EMA, signaling upward continuation. A short position is opened when the fast EMA crosses below the slow EMA.

## Parameters
- **Fast EMA Length** – period for the fast EMA (default: 20).
- **Candle Type** – timeframe of candles (default: 4-hour).
- **Stop Loss** – protective stop loss applied via `StartProtection` (default: 1000).
- **Take Profit** – profit target applied via `StartProtection` (default: 2000).

## How It Works
1. On start the strategy subscribes to the selected candle series and creates two EMA indicators.
2. Each finished candle is processed to detect crossovers between the fast and slow EMAs.
3. A crossover from below to above opens a long position and closes any short one. The opposite crossover opens a short position and closes any long.
4. Risk management is handled through the built-in stop loss and take profit parameters.

This example is a simplified conversion of the original MQL `Exp_TrendContinuation` expert.
