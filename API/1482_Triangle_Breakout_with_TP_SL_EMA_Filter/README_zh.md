# Triangle Breakout Strategy with TP SL and EMA Filter
[English](README.md) | [Русский](README_ru.md)

通过枢轴高点和低点识别三角形形态，向上突破时做多，可选地要求价格高于 EMA20 和 EMA50，并使用百分比止盈止损。

## 细节

- **入场条件**: 收盘价突破三角形上边，且可选 EMA20/EMA50 过滤
- **多空方向**: 多头
- **出场条件**: 百分比止盈或止损
- **止损**: 有
- **默认值**:
  - `PivotLength` = 5
  - `TakeProfitPercent` = 3
  - `StopLossPercent` = 1.5
  - `UseEmaFilter` = true
  - `EmaFast` = 20
  - `EmaSlow` = 50
  - `CandleType` = 1 小时
- **过滤器**:
  - 分类: 形态
  - 方向: 多头
  - 指标: EMA, Pivot
  - 止损: 有
  - 复杂度: 高级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
