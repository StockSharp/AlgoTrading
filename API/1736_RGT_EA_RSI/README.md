# RGT EA RSI Strategy

This strategy combines the **Relative Strength Index (RSI)** with **Bollinger Bands** to identify extreme price movements and trade potential reversals. Positions are opened when the RSI enters oversold or overbought zones and price crosses the Bollinger Bands. A stop loss and trailing stop manage risk and secure profits.

## How It Works

1. Calculate RSI and Bollinger Bands for incoming candles.
2. **Buy** when RSI is below the oversold level and the close price is below the lower band.
3. **Sell** when RSI is above the overbought level and the close price is above the upper band.
4. After entry, a fixed stop loss is placed. Once the position gains the minimum profit, the stop loss trails the price.

## Parameters

| Name | Description |
|------|-------------|
| `Volume` | Order volume. |
| `RsiPeriod` | RSI calculation period. |
| `RsiHigh` | RSI overbought threshold. |
| `RsiLow` | RSI oversold threshold. |
| `StopLoss` | Initial stop loss distance in price units. |
| `TrailingStop` | Trailing stop distance in price units. |
| `MinProfit` | Minimum profit before trailing activates. |
| `CandleType` | Candle type used for calculations. |

## Notes

- Works on any instrument and timeframe supported by StockSharp.
- Uses market orders for entries and exits.
- Trailing stop updates on every completed candle.
