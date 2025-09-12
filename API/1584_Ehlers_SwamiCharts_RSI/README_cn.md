# Ehlers SwamiCharts RSI 指标
[English](README.md) | [Русский](README_ru.md)

汇总周期 2–48 的 RSI 值形成颜色图。平均颜色为绿色时做多，红色时做空。

## 细节

- **入场条件**：平均颜色为绿色（`Color1Avg` == 255 且 `Color2Avg` > `LongColor`）做多；平均颜色为红色（`Color1Avg` > `ShortColor` 且 `Color2Avg` == 255）做空。
- **多空**：双向。
- **出场条件**：相反信号。
- **止损**：否。
- **默认值**：
  - `LongColor` = 50
  - `ShortColor` = 50
  - `CandleType` = 5 分钟
- **过滤器**：
  - 类别: Oscillator
  - 方向: 双向
  - 指标: RSI
  - 止损: 否
  - 复杂度: 高级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
