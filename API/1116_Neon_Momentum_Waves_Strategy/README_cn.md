# Neon Momentum Waves策略
[English](README.md) | [Русский](README_ru.md)

Neon Momentum Waves策略使用MACD直方图的穿越信号进行双向交易。当直方图向上穿越入场水平（默认0）时做多，向下穿越时做空。直方图达到设定的退出水平时平仓。

## 详情

- **入场条件**：MACD直方图穿越入场水平。
- **多空方向**：双向。
- **出场条件**：直方图穿越多/空退出水平。
- **止损**：无。
- **默认值**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 20
  - `EntryLevel` = 0
  - `LongExitLevel` = 11
  - `ShortExitLevel` = -9
  - `CandleType` = 1 分钟
- **筛选**：
  - 类别：动量
  - 方向：双向
  - 指标：MACD
  - 止损：否
  - 复杂度：基础
  - 时间框架：日内 (1m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
