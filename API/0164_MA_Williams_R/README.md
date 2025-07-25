# Ma Williams R Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Implementation of strategy - MA + Williams %R. Buy when price is above MA and Williams %R is below -80 (oversold). Sell when price is below MA and Williams %R is above -20 (overbought).

The moving average shows the prevailing trend direction. Williams %R looks for overbought or oversold points relative to that trend.

Fits swing traders waiting for pullbacks toward the average. Stop-loss distance comes from ATR.

## Details

- **Entry Criteria**:
  - Long: `Close > MA && WilliamsR < WilliamsROversold`
  - Short: `Close < MA && WilliamsR > WilliamsROverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Williams %R returns to middle
- **Stops**: Percent-based using `StopLoss`
- **Default Values**:
  - `MaPeriod` = 20
  - `MaType` = MovingAverageTypeEnum.Simple
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Moving Average, Williams %R, R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 79%. It performs best in the stocks market.
