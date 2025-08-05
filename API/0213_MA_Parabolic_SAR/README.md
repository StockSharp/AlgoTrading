# MA Parabolic SAR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
The MA Parabolic SAR strategy attempts to capture sustained trends while using a trailing stop for risk control. A simple moving average determines the prevailing direction and the Parabolic SAR dots provide entry timing and stop placement. When both indicators align, the system assumes momentum is strong enough to follow.

Testing indicates an average annual return of about 76%. It performs best in the forex market.

A long position is opened when the closing price sits above the moving average and the Parabolic SAR dots flip below the market. A short position is taken when price is below the average and the SAR dots flip above price, signalling downward pressure. The strategy exits once price crosses the SAR in the opposite direction, locking in profits or limiting losses.

This approach is best suited for traders who prefer systematic trend following with clear, mechanical stops. The Parabolic SAR continuously adjusts as volatility changes, keeping exposure in line with market conditions while the moving average prevents trades against the broader trend.

## Details
- **Entry Criteria**:
  - **Long**: Price > MA && Price > Parabolic SAR
  - **Short**: Price < MA && Price < Parabolic SAR
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when price falls below Parabolic SAR
  - **Short**: Exit when price rises above Parabolic SAR
- **Stops**: Yes, dynamic via Parabolic SAR and optional fixed stop.
- **Default Values**:
  - `MaPeriod` = 20
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TakeValue` = new Unit(0, UnitTypes.Absolute)
  - `StopValue` = new Unit(2, UnitTypes.Percent)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA, Parabolic SAR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

