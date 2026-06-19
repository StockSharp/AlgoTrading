# 趋势偏离 BTC
[English](README.md) | [Русский](README_ru.md)

该策略结合 DMI 交叉、布林带以及 Momentum、MACD、SuperTrend、Aroon 等确认信号，在趋势中寻找价格偏离并入场。

## 详情

- **入场条件**: +DI 上穿 -DI 且 价格低于布林上轨，并且 Momentum/MACD/SuperTrend/Aroon 任一确认。
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 否
- **默认值**:
  - `DmiPeriod` = 15
  - `BbLength` = 13
  - `BbMultiplier` = 2.3
  - `MomentumLength` = 10
  - `AroonLength` = 5
  - `MacdFast` = 15
  - `MacdSlow` = 200
  - `MacdSignal` = 25
  - `AtrPeriod` = 200
  - `SuperTrendFactor` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: DMI, Bollinger Bands, Momentum, MACD, SuperTrend, Aroon
  - 止损: 否
  - 复杂度: 高级
  - 时间框架: 日内 (1m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 高
