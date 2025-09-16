# Ichimoku Chinkou Cross Strategy

This strategy trades based on the crossover of the Ichimoku Chinkou Span (lagging line) with price.

## Strategy Logic

- **Long:** Chinkou crosses above price, both the current price and Chinkou are above the Kumo cloud, and RSI is above the `RsiBuyLevel`.
- **Short:** Chinkou crosses below price, both the current price and Chinkou are below the Kumo cloud, and RSI is below the `RsiSellLevel`.

The strategy uses stop-loss protection via `StartProtection` and parameters for Tenkan, Kijun, Senkou Span B, and RSI.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `TenkanPeriod` | Tenkan-sen period | 9 |
| `KijunPeriod` | Kijun-sen period | 26 |
| `SenkouSpanPeriod` | Senkou Span B period | 52 |
| `RsiPeriod` | RSI calculation period | 14 |
| `RsiBuyLevel` | RSI minimum for long | 70 |
| `RsiSellLevel` | RSI maximum for short | 30 |
| `StopLoss` | Stop-loss percent or value | 2% |
| `CandleType` | Candle type for subscription | 5-minute candles |

## Indicators

- Ichimoku
- RSI
