# ZMFX Stolid 5a EA 策略
[English](README.md) | [Русский](README_ru.md)

多时间框趋势策略，通过RSI和随机指标确认回调入场。
系统依据4小时随机指标和1小时平滑均线确定主要趋势。
在RSI超买/超卖的K线反转处开仓，并在相反信号出现时平仓。

## 细节

- **入场条件**：
  - 多头：`UpTrend && PreviousBarDown && PrevRSI < 30 && (RSI15 < 30 => double volume)`
  - 空头：`DownTrend && PreviousBarUp && PrevRSI > 70 && (RSI15 > 70 => double volume)`
- **多空方向**：双向
- **止损**：无固定止损；根据指标条件出场
- **默认参数**：
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：RSI, Stochastic, Smoothed Moving Average
  - 止损：否
  - 复杂度：中等
  - 时间框架：多时间框
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
