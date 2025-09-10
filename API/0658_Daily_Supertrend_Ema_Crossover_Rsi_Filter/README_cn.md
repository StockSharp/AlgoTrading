# 日线 Supertrend EMA 交叉 RSI 过滤 策略
[English](README.md) | [Русский](README_ru.md)

该策略仅在 Supertrend 确认方向且 RSI 条件满足时交易 EMA 交叉。使用基于 ATR 的止损和止盈。

## 细节

- **入场条件**:
  - 多头：`Fast EMA` 上穿 `Slow EMA`，Supertrend 处于上升趋势，`RSI < RsiOverbought`
  - 空头：`Fast EMA` 下穿 `Slow EMA`，Supertrend 处于下降趋势，`RSI > RsiOversold`
- **多空**: 双向
- **出场条件**: ATR 止损或止盈
- **止损**: 是
- **默认值**:
  - `FastEmaLength` = 3
  - `SlowEmaLength` = 6
  - `AtrLength` = 3
  - `StopLossMultiplier` = 2.5m
  - `TakeProfitMultiplier` = 4m
  - `RsiLength` = 10
  - `RsiOverbought` = 65m
  - `RsiOversold` = 30m
  - `SupertrendMultiplier` = 1m
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame()
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: EMA, Supertrend, RSI, ATR
  - 止损: ATR 倍数
  - 复杂度: 中等
  - 时间框架: 长期
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
