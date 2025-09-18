# AMS ES RSI Strategy

## Summary
The AMS ES RSI strategy replicates the behaviour of the MetaTrader expert `Expert_AMS_ES_RSI` inside StockSharp. It combines morning/evening star candlestick formations with a Relative Strength Index (RSI) confirmation filter. Long trades are opened when a bullish morning star appears while RSI indicates oversold conditions. Short trades are taken when a bearish evening star forms in conjunction with an overbought RSI. Positions are closed when RSI crosses back through configurable threshold levels.

## Market Assumptions
- Works on any instrument that produces regular OHLC candles. Spot FX and index futures were the original targets of the MQL expert.
- The strategy expects smooth price action where Japanese candlestick patterns are meaningful. Extremely noisy tick charts may not produce reliable signals.

## Entry Logic
1. Subscribe to the configured timeframe (default: 1 hour) and wait for three fully closed candles.
2. Compute the average body size across the last *BodyAveragePeriod* candles (default: 3).
3. Detect a **Morning Star** when:
   - Candle 3 is strongly bearish (`Open - Close` larger than the averaged body size).
   - Candle 2 has a small real body (less than half of the average) and gaps below candle 3.
   - Candle 1 closes above the midpoint of candle 3.
4. Detect an **Evening Star** with the symmetric bearish conditions.
5. Confirm long entries when the current RSI value is below *LongEntryRsi* (default: 40). Confirm short entries when RSI is above *ShortEntryRsi* (default: 60).
6. Execute market orders using the strategy `Volume`.

## Exit Logic
- Close long positions when RSI crosses downward through *UpperExitRsi* (default: 70) or *LowerExitRsi* (default: 30).
- Close short positions when RSI crosses upward through the same levels.
- No hard stop-loss or take-profit is applied. Risk management must be handled externally or by adjusting the thresholds.

## Parameters
| Name | Description | Default | Range |
| ---- | ----------- | ------- | ----- |
| `CandleType` | Data type representing the candle series to subscribe. | 1-hour time frame | Any supported candle type |
| `RsiPeriod` | RSI calculation length. | 47 | Optimisable (10–70) |
| `BodyAveragePeriod` | Number of candles used to calculate the average body size required for pattern validation. | 3 | Optimisable (2–6) |
| `LongEntryRsi` | Maximum RSI value that allows long entries. | 40 | Optimisable (20–50) |
| `ShortEntryRsi` | Minimum RSI value that allows short entries. | 60 | Optimisable (50–80) |
| `LowerExitRsi` | Lower boundary that triggers exits when crossed upward. | 30 | Optimisable (20–40) |
| `UpperExitRsi` | Upper boundary that triggers exits when crossed downward. | 70 | Optimisable (60–80) |

## Implementation Notes
- Uses the StockSharp high-level API with automatic candle subscriptions.
- Relies solely on indicator values provided by `Bind`, avoiding manual `GetValue` calls in accordance with the project guidelines.
- Maintains only a minimal in-memory history (three recent candles) for pattern validation.
- The strategy automatically calls `StartProtection()` on launch to enable built-in safety mechanisms.

## Usage Tips
1. Attach the strategy to an instrument/portfolio pair and ensure that the candle series is available from your connector.
2. Tune RSI levels according to the asset volatility. Wider thresholds reduce the number of trades but increase confirmation quality.
3. Combine with external position sizing modules (e.g., risk-based volume) to emulate the fixed lot behaviour of the original EA.
4. When backtesting, ensure the candle data contains gaps so that the star patterns can be correctly identified.
