# Turtle Trading 策略
[English](README.md) | [Русский](README_ru.md)

经典的 Turtle Trading 系统，使用唐奇安通道突破和基于 ATR 的风险管理。

## 细节

- **入场条件**: 唐奇安通道上轨/下轨突破
- **多空方向**: 双向
- **出场条件**: 短期唐奇安通道穿越或移动止损
- **止损**: 基于 ATR 的初始止损和移动止损
- **默认值**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `EntryLengthMode2` = 55
  - `ExitLengthMode2` = 20
  - `AtrPeriod` = 14
  - `RiskPerTrade` = 0.02
  - `InitialStopAtrMultiple` = 2
  - `PyramidAtrMultiple` = 0.5
  - `MaxUnits` = 4
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: DonchianChannels, ATR
  - 止损: ATR
  - 复杂度: 高级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
