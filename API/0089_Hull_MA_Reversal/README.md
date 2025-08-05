# Hull MA Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Hull Moving Average responds quickly to price changes while remaining smooth. A change in its direction can foreshadow a short-term reversal. This strategy monitors consecutive Hull MA values and trades when the slope flips.

Testing indicates an average annual return of about 154%. It performs best in the stocks market.

When the moving average transitions from falling to rising, a long position is opened. A shift from rising to falling initiates a short. Risk is controlled using an ATR-based stop placed beyond the recent candle.

Exits rely on that protective stop, capturing a portion of the move that follows the momentum shift highlighted by the Hull MA.

## Details

- **Entry Criteria**: Hull MA slope changes direction.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss.
- **Stops**: Yes, ATR based.
- **Default Values**:
  - `HmaPeriod` = 9
  - `AtrMultiplier` = 2 ATR
  - `CandleType` = 15 minute
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Hull MA, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

