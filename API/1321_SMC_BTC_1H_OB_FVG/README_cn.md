# SMC Strategy BTC 1H OB FVG
[English](README.md) | [Русский](README_ru.md)

基于 Smart Money Concepts 的比特币1小时策略。在出现看涨结构突破后，价格回到识别出的订单块或公平价值缺口时做多。止损使用 ATR 乘数，止盈根据风险回报比计算。

## 细节

- **入场条件**：看涨 BOS 后，在 `ZoneTimeout` 根K内触及 OB 或 FVG 时买入。
- **多/空**：仅做多。
- **出场条件**：固定止盈和止损。
- **止损**：基于 ATR 的固定值。
- **默认参数**：
  - `UseOrderBlock` = true
  - `UseFvg` = true
  - `AtrFactor` = 6
  - `RiskRewardRatio` = 2.5
  - `ZoneTimeout` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**：
  - 类别：趋势
  - 方向：多头
  - 指标：ATR
  - 止损类型：固定
  - 复杂度：简单
  - 时间框架：日内 (1H)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
