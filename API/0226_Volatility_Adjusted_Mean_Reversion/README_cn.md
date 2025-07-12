# Volatility Adjusted Mean Reversion Strategy

此均值回归变体根据ATR与标准差之比调整入场阈值。若相对噪声的波动增大，触发交易所需的距离也随之增加，避免在剧烈波动时过早进场。

当价格低于均线超过调整后的阈值时做多；价格高于均线同样距离时做空。价格重新靠近均线时离场。止损等于2倍ATR，在等待回归期间控制风险。

该自适应阈值适用于波动性变化较大的市场。

## 细节
- **入场条件**:
  - 多头: `Price < MA - Multiplier * ATR/StdDev`
  - 空头: `Price > MA + Multiplier * ATR/StdDev`
- **多/空**: 双向
- **离场条件**:
  - 多头: 收盘价>= MA
  - 空头: 收盘价<= MA
- **止损**: 基于ATR动态设置
- **默认值**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mean Reversion
  - 方向: 双向
  - 指标: ATR, StdDev
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
