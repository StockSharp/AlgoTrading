# EMA Pullback Speed 策略
[English](README.md) | [Русский](README_ru.md)

EMA Pullback Speed 策略使用根据价格加速度自适应的动态 EMA。价格在上升趋势中回调至动态 EMA 并出现看涨反转且速度达到阈值时开多仓。相反条件下开空仓。退出使用基于 ATR 的止损和固定百分比的止盈。

## 细节

- **入场条件**:
  - **多头**: 价格高于动态 EMA，看涨反转，价格回到 EMA，速度为正，短期 EMA 高于长期 EMA，速度 ≥ `LongSpeedMin`。
  - **空头**: 价格低于动态 EMA，看跌反转，价格回到 EMA，速度为负，短期 EMA 低于长期 EMA，速度 ≤ `ShortSpeedMax`。
- **多空**: 双向。
- **出场条件**: ATR 止损和固定百分比止盈。
- **止损/止盈**: 止损 `AtrMultiplier`×ATR，止盈 `FixedTpPct`%。
- **默认值**:
  - `MaxLength` = 50
  - `AccelMultiplier` = 3
  - `ReturnThreshold` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 4
  - `FixedTpPct` = 1.5
  - `ShortEmaLength` = 21
  - `LongEmaLength` = 50
  - `LongSpeedMin` = 1000
  - `ShortSpeedMax` = -1000
- **过滤器**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: EMA, ATR
  - 止损: ATR 止损, 固定止盈
  - 复杂度: 中等
  - 时间框架: 5m
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
