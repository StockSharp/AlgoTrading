# ColorMaRsi Trigger Strategy

This strategy is a StockSharp port of the original MQL5 expert `exp_colormarsi-trigger.mq5`. It compares fast and slow EMAs and fast and slow RSI values. The combined signal takes values -1, 0 or +1. A position is opened when the previous signal has the opposite sign to the current one.

## How it works

- When the signal turns from positive to zero or negative, a long position is opened and any short position is closed.
- When the signal turns from negative to zero or positive, a short position is opened and any long position is closed.

## Parameters

- **Fast EMA** – period for the fast exponential moving average.
- **Slow EMA** – period for the slow exponential moving average.
- **Fast RSI** – period for the fast RSI.
- **Slow RSI** – period for the slow RSI.
- **Candle Type** – timeframe of the candles used for calculation.

## Indicators

- Exponential Moving Average (fast and slow)
- Relative Strength Index (fast and slow)

Only finished candles are processed. Orders are placed using `BuyMarket` and `SellMarket`.
