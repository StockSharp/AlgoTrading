# Game Theory Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Game Theory Trading Strategy blends herd behavior analysis, liquidity trap detection, institutional flow and Nash equilibrium zones to trade contrarian and momentum moves.

The strategy watches RSI extremes and volume spikes to spot herd buying or selling. Liquidity traps around recent highs and lows plus accumulation/distribution and smart money bias refine entries. Price bands built from a moving average and standard deviation define Nash equilibrium for reversion trades. Position size adapts when price is near equilibrium or institutional volume appears.

## Details
- **Data**: Price and volume candles.
- **Entry Criteria**: Contrarian, momentum or Nash reversion signals.
- **Exit Criteria**: Stop loss / take profit or opposite signals.
- **Stops**: Optional stop loss and take profit.
- **Default Values**:
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `HerdThreshold` = 2.0
  - `LiquidityLookback` = 50
  - `InstVolumeMultiplier` = 2.5
  - `InstMaLength` = 21
  - `NashPeriod` = 100
  - `NashDeviation` = 0.02
  - `UseStopLoss` = True
  - `StopLossPercent` = 2
  - `UseTakeProfit` = True
  - `TakeProfitPercent` = 5
- **Filters**:
  - Category: Mixed contrarian/momentum
  - Direction: Long & Short
  - Indicators: RSI, SMA, Accumulation/Distribution, StandardDeviation, Highest/Lowest
  - Complexity: Advanced
  - Risk level: Medium
