# EMA 34 均线突破并带有保本止损
[English](README.md) | [Русский](README_ru.md)

**EMA 34 Crossover with Break Even Stop Loss** 策略在价格向上穿越34周期EMA时做多。止损放在上一根K线的最低点，止盈设为风险的十倍，当价格达到三倍风险时，止损移动到入场价。

## 细节
- **入场条件**：收盘价由下向上突破 EMA(34)。
- **多空方向**：仅做多。
- **离场条件**：上一根最低点的止损或10倍风险的止盈。
- **止损**：是，带保本机制。
- **默认参数**：
  - `EmaPeriod = 34`
  - `TakeProfitMultiplier = 10m`
  - `BreakEvenMultiplier = 3m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **过滤器**：
  - 类别: 趋势跟随
  - 方向: 多头
  - 指标: EMA
  - 止损: 是
  - 复杂度: 初级
  - 时间框架: 日内 (5分)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险水平: 中等
