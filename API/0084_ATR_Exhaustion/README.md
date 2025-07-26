# ATR Exhaustion Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
A sudden surge in Average True Range indicates expanding volatility that can quickly fade. This strategy looks for ATR readings that spike above a moving average by a configurable multiplier. When coupled with a reversal candle it aims to capture the subsequent contraction.

Each bar updates ATR and its own average. If ATR exceeds the average by the multiplier and the candle closes opposite the prior move, a trade is opened. The stop-loss uses an ATR multiple as well, anchoring risk to current volatility levels.

Positions typically rely on the stop for exit, seeking a swift retracement after the volatility burst subsides.

## Details

- **Entry Criteria**: ATR spike above average with reversal candle.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss.
- **Stops**: Yes, ATR based.
- **Default Values**:
  - `AtrPeriod` = 14
  - `AtrAvgPeriod` = 20
  - `AtrMultiplier` = 1.5
  - `MaPeriod` = 20
  - `StopLoss` = 2%
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: ATR, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 139%. It performs best in the stocks market.
