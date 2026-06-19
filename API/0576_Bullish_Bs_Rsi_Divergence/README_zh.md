# Bullish B's RSI Divergence
[English](README.md) | [Русский](README_ru.md)

使用RSI和枢轴点检测常规与隐藏的看涨背离。出现金叉信号时开多单，遇到看跌信号、RSI触及目标或触发跟踪止损时平仓。

## 细节

- **入场条件**：
  - **多头**：RSI出现常规或隐藏的看涨背离。
- **方向**：仅做多。
- **出场条件**：看跌背离、RSI突破目标或跟踪止损触发。
- **止损**：可选的ATR或百分比跟踪止损。
- **默认值**：
  - `RsiPeriod` = 9
  - `PivotLookbackRight` = 3
  - `PivotLookbackLeft` = 1
  - `TakeProfitRsiLevel` = 80
  - `RangeUpper` = 60
  - `RangeLower` = 5
  - `StopType` = None
  - `StopLoss` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 3.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 类别：背离
  - 方向：多头
  - 指标：RSI, ATR
  - 止损：可选跟踪止损
  - 复杂度：高级
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：是
  - 风险等级：中等
