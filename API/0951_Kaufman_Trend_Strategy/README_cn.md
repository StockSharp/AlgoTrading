# Kaufman Trend 策略
[English](README.md) | [Русский](README_ru.md)

**Kaufman Trend Strategy** 使用卡尔曼滤波器估计价格及其速度。趋势强度由速度计算，并在最近窗口内归一化。当趋势强度达到阈值且价格位于滤波值之上或之下时触发进场。止损基于最近摆动加减 ATR，利润在动能减弱时分批了结。

## 细节
- **入场条件**：趋势强度阈值与价格相对滤波值的位置。
- **多/空**：双向。
- **出场条件**：分批止盈与趋势减弱或触发止损。
- **止损**：是，最近低/高点 ± ATR。
- **默认值**：
  - `TakeProfit1Percent = 50`
  - `TakeProfit2Percent = 25`
  - `TakeProfit3Percent = 25`
  - `SwingLookback = 10`
  - `AtrPeriod = 14`
  - `TrendStrengthEntry = 60`
  - `TrendStrengthExit = 40`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：卡尔曼
  - 止损：是
  - 复杂度：中等
  - 时间框架：日内 (15m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险水平：中等
