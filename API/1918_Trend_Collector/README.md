# Trend Collector Strategy

This strategy is a conversion of the original MQL `TrendCollector.mq4` algorithm. It combines trend detection using two exponential moving averages with momentum and volatility filters.

## Strategy Logic

- **Fast EMA vs Slow EMA** – The strategy follows the main trend by comparing a fast EMA with a slow EMA.
- **Stochastic Oscillator** – Determines overbought and oversold conditions. Long positions open when the stochastic value is below the lower threshold and the fast EMA is above the slow EMA. Short positions trigger when the stochastic value is above the upper threshold and the fast EMA is below the slow EMA.
- **ATR Volatility Filter** – Trades only occur when the current ATR value is below the volatility limit, avoiding highly volatile periods.
- **Trading Window** – Orders are generated only between the configured start and end hours.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| FastMaLength | Period of the fast EMA | 4 |
| SlowMaLength | Period of the slow EMA | 204 |
| StochasticPeriod | Period for the stochastic oscillator | 14 |
| StochasticUpper | Upper level for stochastic | 80 |
| StochasticLower | Lower level for stochastic | 20 |
| AtrPeriod | Period for ATR | 14 |
| AtrLimit | Maximum ATR value allowed to trade | 2 |
| StartHour | Start hour for trading window | 5 |
| EndHour | End hour for trading window | 24 |
| CandleTimeFrame | Time frame of processed candles | 5 minutes |

The Python version is currently not provided.
