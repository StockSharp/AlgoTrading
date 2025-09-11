# 多头 MTF EMA 云
[English](README.md) | [Русский](README_ru.md)

当短期 EMA 上穿长期 EMA 时开多的 EMA 云交叉策略。使用固定百分比止损和止盈。

## 详情

- **入场条件**: 短期 EMA 上穿长期 EMA。
- **多/空方向**: 仅多头。
- **退出条件**: 价格触及止损或止盈。
- **止损**: 固定百分比止损和止盈。
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `ShortLength` = 21
  - `LongLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 2.0m
- **过滤器**:
  - 类型: Trend-following
  - 方向: Long
  - 指标: EMA
  - 止损: 是
  - 复杂度: 初级
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中
