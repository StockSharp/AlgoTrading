# 布林带追踪止损
[English](README.md) | [Русский](README_ru.md)

当收盘价突破上轨时做多。
当收盘价跌破下轨或触发基于ATR的追踪止损时平仓。

## 详情

- **入场条件**: 收盘价高于上轨。
- **多空方向**: 仅多头。
- **出场条件**: 收盘价低于下轨或触发追踪止损。
- **止损**: 追踪止损。
- **默认值**:
  - `BbLength` = 20
  - `BbDeviation` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: Long
  - 指标: Bollinger Bands, ATR
  - 止损: Yes
  - 复杂度: Beginner
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
