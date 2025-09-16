# CaiChannel System Digit Strategy

This strategy is a simplified StockSharp port of the MetaTrader expert **i-CAiChannel System Digit**.

The algorithm monitors a volatility channel built from a moving average and standard deviation (Bollinger Bands).
When a candle closes outside the channel and the next candle returns inside, the strategy trades in the direction of the re-entry.

## Parameters
- `Length` – period of the moving average.
- `Width` – standard deviation multiplier.
- `Candle Type` – timeframe for processing.

## Trading Logic
1. Subscribe to candles of the selected timeframe.
2. Calculate Bollinger Bands with the specified parameters.
3. If the previous candle closed above the upper band and the current candle closes back inside, go long.
4. If the previous candle closed below the lower band and the current candle closes back inside, go short.
5. The position is reversed when the opposite signal occurs.

All signals are generated only on finished candles.
