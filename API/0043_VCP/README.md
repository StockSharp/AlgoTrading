# Volatility Contraction Pattern
[Русский](README_ru.md) | [中文](README_cn.md)
 
The VCP strategy looks for a sequence of narrowing price ranges. As each range contracts, energy builds for a breakout. The system measures range size and waits for a break above the highest high or below the lowest low.

Testing indicates an average annual return of about 166%. It performs best in the stocks market.

Once contraction is observed, a breakout beyond the recent extremes triggers a trade in that direction. Price crossing the moving average is used to manage exits.

This approach aims to capture explosive moves following a volatility squeeze.

## Details

- **Entry Criteria**: Range contraction then breakout of recent high/low.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crosses MA or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Range, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

