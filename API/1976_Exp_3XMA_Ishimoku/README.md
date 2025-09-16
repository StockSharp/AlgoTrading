# Exp 3XMA Ichimoku Strategy

This strategy is a conversion of the MQL expert `exp_3xma_ishimoku`. It uses the Ichimoku indicator with shortened periods and acts contrarian to cloud breakouts.

The Kijun line is compared to the Ichimoku cloud boundaries. When Kijun falls from above the cloud into it, the strategy closes short positions and opens a long one if buying is allowed. When Kijun rises from below the cloud into it, long positions are closed and a short position may be opened.

The default timeframe for analysis is 4-hour candles.

## Parameters
- **Tenkan Period** – length of the Tenkan-sen line.
- **Kijun Period** – length of the Kijun-sen line.
- **Senkou Span B Period** – period for the second Senkou span.
- **Allow Buy** – enable opening long positions.
- **Allow Sell** – enable opening short positions.
- **Candle Type** – candle series used for indicator calculation.

## How It Works
1. Subscribes to the selected candle series and binds the Ichimoku indicator.
2. Processes only finished candles.
3. Detects when the Kijun line crosses the cloud borders.
4. Closes opposite positions and opens a new one in the signal direction if allowed.

## Disclaimer
This example is for educational purposes and does not constitute financial advice. Use at your own risk.
