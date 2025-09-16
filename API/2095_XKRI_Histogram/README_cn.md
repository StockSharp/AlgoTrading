# XKRI Histogram 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 Kairi Relative Index 指标，并使用指数移动平均进行平滑。系统在平滑后的振荡器出现局部高点或低点时识别反转模式并开仓做多或做空。

## 细节

- **入场条件**：
  - 做多：`Kri[1] < Kri[2] && Kri[0] > Kri[1]`
  - 做空：`Kri[1] > Kri[2] && Kri[0] < Kri[1]`
- **多空方向**：双向
- **止损/止盈**：按点数设定
- **默认值**：
  - `KriPeriod` = 20
  - `SmoothPeriod` = 7
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **过滤器**：
  - 类别：反转
  - 方向：双向
  - 指标：Kairi、EMA
  - 止损：是
  - 复杂度：初级
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
