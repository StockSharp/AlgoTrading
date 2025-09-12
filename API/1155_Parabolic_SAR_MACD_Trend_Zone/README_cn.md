# 抛物线SAR与MACD确认
[English](README.md) | [Русский](README_ru.md)

该策略将抛物线SAR指标与MACD结合。当价格与SAR交叉且得到MACD同向确认时开仓，以捕捉趋势反转。

## 详情

- **入场条件**: 价格穿越SAR且MACD线位于信号线上方或下方同向。
- **多空方向**: 双向。
- **出场条件**: 价格/SAR或MACD的反向交叉。
- **止损**: 无。
- **默认值**:
  - `SarStart` = 0.02m
  - `SarIncrement` = 0.02m
  - `SarMax` = 0.2m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: Parabolic SAR, MACD
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
