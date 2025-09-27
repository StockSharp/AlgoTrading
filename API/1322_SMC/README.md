[Русский](README_ru.md) | [中文](README_cn.md)

SMC Strategy defines premium, equilibrium, and discount zones from recent swing highs and lows. It trades in discount or premium zones with an SMA trend filter and simple order block confirmation.

## Details

- **Entry Criteria**: price in discount zone above SMA with order block support; price in premium zone below SMA with order block resistance
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `SwingHighLength` = 8
  - `SwingLowLength` = 8
  - `SmaLength` = 50
  - `OrderBlockLength` = 20
- **Filters**:
  - Category: Zone
  - Direction: Both
  - Indicators: Highest, Lowest, SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
