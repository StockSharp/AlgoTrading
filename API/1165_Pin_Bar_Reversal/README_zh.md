# Pin Bar Reversal 策略
[English](README.md) | [Русский](README_ru.md)

利用带有趋势过滤的 Pin Bar 蜡烛，并根据 ATR 设置止损和止盈。SMA 上方的看涨 Pin Bar 开多，SMA 下方的看跌 Pin Bar 开空，波动性太低时跳过。

## 细节

- **入场条件**：顺势的 Pin Bar，长影线、短实体，且 ATR 大于 `MinAtr`。
- **多空**：双向。
- **出场条件**：基于 ATR 的止损或止盈。
- **止损**：是，ATR 倍数。
- **默认值**：
  - `TrendLength` = 50
  - `MaxBodyPct` = 0.30
  - `MinWickPct` = 0.66
  - `AtrLength` = 14
  - `StopMultiplier` = 1
  - `TakeMultiplier` = 1.5
  - `MinAtr` = 0.0015
  - `CandleType` = 1 小时
- **过滤器**：
  - 类别: Pattern
  - 方向: 双向
  - 指标: SMA, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
