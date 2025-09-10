# SMC Order Block Zones Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy identifies swing highs and lows to define premium and discount zones. A simple moving average acts as a trend filter and recent order blocks confirm entries. Trades are executed when price moves from one zone toward equilibrium with order block confirmation, using a percentage stop loss for protection.

## Details

- **Entry Criteria**:
  - Close below equilibrium but above discount zone and SMA for long trades.
  - Close above equilibrium but below premium zone and SMA for short trades.
  - Price must touch the respective order block level.
- **Long/Short**: Configurable long, short, or both.
- **Exit Criteria**: Opposite signal or stop loss.
- **Stops**: Percent stop loss.
- **Default Values**:
  - `SwingHighLength` = 8
  - `SwingLowLength` = 8
  - `SmaLength` = 50
  - `OrderBlockLength` = 20
  - `StopLossPercent` = 2
- **Filters**:
  - Category: Trend and SMC
  - Direction: User-defined
  - Indicators: SMA, Highest, Lowest
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
