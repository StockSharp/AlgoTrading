# DVD Level Strategy

This strategy is a simplified translation of the original "DVD Level" MQL5 expert advisor. It employs the Range Action Verification Index (RAVI) to determine market direction. RAVI is calculated using 2- and 24-period exponential moving averages on 1-hour candles.

## Parameters
- `Volume` – order volume used for trades.

## Logic
1. Subscribe to 1-hour candles and compute EMA(2) and EMA(24).
2. Calculate `RAVI = (EMA2 - EMA24) / EMA24 * 100`.
3. If RAVI crosses below zero, the strategy buys if it is flat or short.
4. If RAVI crosses above zero, the strategy sells if it is flat or long.
5. Built-in position protection is activated via `StartProtection()`.

The approach captures potential reversals when short-term momentum diverges from the longer-term trend.
