# Backtest UT Bot + RSI 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合UT Bot趋势指标与RSI水平。当UT Bot向上反转且RSI低于阈值时做多；当UT Bot向下反转且RSI高于阈值时做空。

## 细节

- **入场条件**：
  - **多头**：UT Bot转为上行且RSI < `RSI Oversold`。
  - **空头**：UT Bot转为下行且RSI > `RSI Overbought`。
- **多空方向**：双向。
- **出场条件**：
  - 百分比止盈或止损。
- **止损**：止盈和止损。
- **默认值**：
  - `RSI Length` = 14
  - `RSI Overbought` = 60
  - `RSI Oversold` = 40
  - `ATR Length` = 10
  - `UT Bot Factor` = 1.0
  - `Take Profit %` = 3.0
  - `Stop Loss %` = 1.5
- **筛选**：
  - 类别: Trend Following
  - 方向: 双向
  - 指标: 多个
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
