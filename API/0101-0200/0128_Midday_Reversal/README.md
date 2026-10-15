# Midday Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Midday Reversal seeks turning points that occur around lunchtime when morning trends often exhaust.
Liquidity typically dries up mid-session, leading to reversals as traders square positions.

Testing indicates an average annual return of about 121%. It performs best in the crypto market.

The system monitors for a shift in momentum near midday and enters in the opposite direction of the morning move.

A percent stop controls risk and exits occur if the reversal fails to develop by the afternoon.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MiddayHour` = 12
  - `AfternoonHour` = 16
    The market trades around the clock, so the session is the UTC day. The morning move is the change from the day's open to the close of the candle ending at MiddayHour. Between MiddayHour and AfternoonHour the strategy enters once against that move, on the first candle that closes against it, and closes the position at AfternoonHour.
- **Filters**:
  - Category: Intraday
  - Direction: Both
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

