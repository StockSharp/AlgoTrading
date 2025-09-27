# Random Bias Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Random Bias Trader Strategy emulates the MetaTrader "random trader" expert advisor using StockSharp's high-level API.
At every finished candle the strategy flips a virtual coin and opens a position in that direction when no trade is active.
Stop-loss and take-profit levels are derived either from ATR(10) or from a fixed pip distance and sized by the reward-to-risk ratio.
Position size is computed from the configured risk percentage and automatically capped by the instrument volume limits.
An optional breakeven trigger can move the stop-loss to the entry price once a specified pip gain is reached.

## Details
- **Data**: One candle subscription defined by `CandleType`.
- **Entry Criteria**:
  - Long: No open position, coin toss returns long. Entry price equals the latest close.
  - Short: No open position, coin toss returns short. Entry price equals the latest close.
- **Exit Criteria**:
  - Stop-loss: Distance equals `LossPipDistance` × pip size or `LossAtrMultiplier` × ATR(10) depending on `LossType`.
  - Take-profit: Stop distance multiplied by `RewardRiskRatio`.
  - Breakeven: When enabled, move stop to entry after `BreakevenDistancePips` gain.
- **Stops**: Dynamic stop-loss and take-profit per trade, breakeven stop optional.
- **Default Values**:
  - `CandleType` = 1 minute timeframe
  - `RewardRiskRatio` = 2.0
  - `LossType` = Pip
  - `LossAtrMultiplier` = 5.0
  - `LossPipDistance` = 20 pips
  - `RiskPercentPerTrade` = 1%
  - `UseBreakeven` = Enabled
  - `BreakevenDistancePips` = 10 pips
  - `UseMaxMargin` = Enabled
- **Filters**:
  - Category: Randomized trend-neutral
  - Direction: Both, determined per flip
  - Indicators: ATR(10) (optional)
  - Complexity: Beginner
  - Risk level: Medium, depends on stop sizing

## Notes
- When the risk-based volume becomes too small, the strategy optionally falls back to the maximum tradable volume.
- Stop and target levels are rounded to the instrument price step before orders are placed.
- Breakeven logic keeps only one position open at any time, mirroring the original MetaTrader logic.
