# PA Oscillator Strategy

This strategy is a port of the MQL5 expert **Exp_PA_Oscillator.mq5**. It applies two exponential moving averages (EMAs) to the candle close prices and analyses the derivative of their difference.

## Logic

1. Calculate fast and slow EMAs.
2. Compute the difference between them and track its change from the previous value.
3. Determine a color code for the derivative:
   - **0** – derivative is positive and MACD is rising.
   - **1** – derivative is zero.
   - **2** – derivative is negative and MACD is falling.
4. Use the colors of the two last finished candles to generate signals:
   - Two bars ago had color `0` and the previous bar changed away from `0` → open long and close short positions.
   - Two bars ago had color `2` and the previous bar changed away from `2` → open short and close long positions.

## Parameters

| Name | Description |
| ---- | ----------- |
| `FastLength` | Length of the fast EMA. |
| `SlowLength` | Length of the slow EMA. |
| `BuyPosOpen` | Enable opening long positions. |
| `SellPosOpen` | Enable opening short positions. |
| `BuyPosClose` | Enable closing long positions. |
| `SellPosClose` | Enable closing short positions. |
| `CandleType` | Candle timeframe used for calculations. |

## Notes

- Only finished candles are processed.
- Market orders are used for entries and exits.
- This implementation focuses on clarity and educational purposes rather than profitability.
