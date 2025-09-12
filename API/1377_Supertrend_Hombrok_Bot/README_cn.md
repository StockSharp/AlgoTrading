# Supertrend Hombrok Bot 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 Supertrend，结合成交量、蜡烛实体和 RSI 过滤，并使用 ATR 设置止损和止盈。

## 详情
- **入场条件**：上升趋势且满足成交量与实体过滤并且 RSI 低于超买线做多；下降趋势且满足过滤并 RSI 高于超卖线做空
- **多空方向**：双向
- **离场条件**：基于 ATR 的止损或止盈
- **止损**：固定的 ATR 止损和止盈
- **默认值**：
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70m
  - `RsiOversold` = 30m
  - `VolumeMultiplier` = 1.2m
  - `BodyPctOfAtr` = 0.3m
  - `RiskRewardRatio` = 2m
  - `CapitalPerTrade` = 10m
- **过滤器**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：Supertrend、RSI、ATR、Volume
  - 止损：是
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
