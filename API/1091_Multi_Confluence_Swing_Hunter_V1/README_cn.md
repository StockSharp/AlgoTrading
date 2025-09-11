# Multi-Confluence Swing Hunter V1 策略
[English](README.md) | [Русский](README_ru.md)

Multi-Confluence Swing Hunter V1 策略使用评分系统结合 RSI、MACD 与价格行为来识别波段低点和高点。当看涨信号的分数达到最小入场值时开多仓，在看跌信号分数达到出场阈值时平仓。

## 细节

- **入场条件**：RSI/MACD 信号与看涨蜡烛结构累积分数 ≥ `MinEntryScore`。
- **多空方向**：仅做多。
- **出场条件**：RSI/MACD 信号与看跌蜡烛结构累积分数 ≥ `MinExitScore`。
- **止损**：无。
- **默认值**：
  - `MacdFast` = 3
  - `MacdSlow` = 10
  - `MacdSignal` = 3
  - `RsiLength` = 21
  - `MinEntryScore` = 13
  - `MinExitScore` = 13
  - `MinLowerWickPercent` = 50
  - `RsiOversold` = 30
  - `RsiExtremeOversold` = 25
  - `RsiOverbought` = 70
  - `RsiExtremeOverbought` = 75
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**：
  - 类别：反转
  - 方向：Long
  - 指标：RSI, MACD
  - 止损：无
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
