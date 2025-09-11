# Elliott's Quadratic Momentum
[English](README.md) | [Русский](README_ru.md)

**Elliott's Quadratic Momentum** 策略使用多个 SuperTrend 指标来捕捉波浪动量。

当四条 SuperTrend 线全部指向上升趋势时做多，当全部指向下降趋势时做空。当任意一条 SuperTrend 反转时平仓。

## 详情
- **入场条件**：所有 SuperTrend 指标方向一致。
- **多空方向**：双向。
- **退出条件**：任意 SuperTrend 转向。
- **止损**：无明确止损。
- **默认值**：
  - `AtrLength1 = 7`
  - `Multiplier1 = 4.0m`
  - `AtrLength2 = 14`
  - `Multiplier2 = 3.618m`
  - `AtrLength3 = 21`
  - `Multiplier3 = 3.5m`
  - `AtrLength4 = 28`
  - `Multiplier4 = 3.382m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **过滤器**：
  - 分类: 趋势跟随
  - 方向: 双向
  - 指标: SuperTrend
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
