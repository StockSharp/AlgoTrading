# AcceleratorBot USDJPY H4 策略
[English](README.md) | [Русский](README_ru.md)

AcceleratorBot 策略是原始 MQL4 专家的转换版本，适用于 USDJPY 在 H4 周期。它结合了平均趋向指数 (ADX) 的趋势强度、随机振荡器的动量以及多周期的加速/减速 (AC) 指标。蜡烛图形态被用作方向性过滤器。

## 详情

- **入场条件**：趋势或动量信号，经由蜡烛图过滤确认。
- **方向**：做多和做空。
- **出场条件**：反向信号、止损、止盈或移动止损。
- **止损**：固定和移动。
- **默认值**：
  - `StopLossPoints` = 750
  - `TakeProfitPoints` = 9999
  - `TrailPoints` = 0
  - `AdxPeriod` = 14
  - `AdxThreshold` = 20m
  - `X1` = 0
  - `X2` = 150
  - `X3` = 500
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 类别：趋势与动量
  - 方向：双向
  - 指标：ADX、Stochastic、AC
  - 止损：支持
  - 复杂度：高级
  - 时间框架：H4
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
