# AH HM MFI Strategy

## Summary

The AH HM MFI strategy trades hammer and hanging man candlestick patterns that are confirmed by the Money Flow Index (MFI). When a bullish hammer appears in a short-term downtrend and the MFI stays below an oversold threshold, the strategy opens a long position. When a bearish hanging man forms in an uptrend while the MFI is above an overbought threshold, it opens a short position. Protective exits are triggered when the MFI crosses predefined upper or lower boundaries.

## Core Logic

1. Subscribe to the configured time-frame candles and calculate two indicators:
   - **Money Flow Index** with a configurable period (default: 47).
   - **Simple Moving Average** of closing prices to approximate the trend filter from the original MQL strategy (default length: 5).
2. Detect **hammer** and **hanging man** patterns:
   - Candle body located in the upper third of the range.
   - Long lower shadow relative to the real body.
   - Gap in the direction of the trend compared with the previous candle.
   - Trend confirmation using the midpoint of the previous candle versus the moving average.
3. Confirm entries with MFI thresholds:
   - Enter long if a hammer is detected and the MFI is at or below the configured oversold level (default: 40).
   - Enter short if a hanging man is detected and the MFI is at or above the configured overbought level (default: 60).
4. Manage exits using MFI crossings:
   - Close short positions when the MFI crosses upward above the lower or upper exit levels (defaults: 30 and 70).
   - Close long positions when the MFI crosses upward above the upper exit level or downward below the lower exit level.
5. Start the built-in risk protection module to handle emergency stops.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle data type and timeframe used for pattern detection. | 30-minute time frame |
| `MfiPeriod` | Lookback period for the MFI calculation. | 47 |
| `MaPeriod` | Length of the SMA applied to closing prices for trend confirmation. | 5 |
| `HammerEntryThreshold` | Maximum MFI value allowed before entering on a hammer signal. | 40 |
| `HangingEntryThreshold` | Minimum MFI value required before entering on a hanging man signal. | 60 |
| `MfiUpperExitLevel` | Upper MFI boundary; crossing above it closes any open position. | 70 |
| `MfiLowerExitLevel` | Lower MFI boundary; crossing below it closes long positions, while crossing above it closes shorts. | 30 |

## Notes

- The strategy evaluates only finished candles to avoid acting on incomplete information.
- Hammer and hanging man detection is conservative: both a long lower shadow and a body located near the candle high are required.
- The moving average replaces the MetaTrader 5 `CloseAvg` filter from the original expert advisor, ensuring that entries align with the broader trend.
