# Honest Volatility Grid
[English](README.md) | [Русский](README_ru.md)

该策略使用多个 Keltner 通道水平构建波动率网格，在预设的带区逐步开仓做多和做空，并在相反带或紧急止损时平仓。

## 细节

- **入场条件**：价格到达设定的 Keltner 通道水平。
- **多空方向**：双向。
- **出场条件**：相反通道或紧急止损。
- **止损**：可选的紧急止损。
- **默认值**：
  - `EmaPeriod` = 200
  - `Multiplier` = 1.0
  - `LEntry1Level` = -2
  - `SEntry1Level` = 2
  - `RawStopLevel` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别：Grid
  - 方向：双向
  - 指标：EMA, ATR
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内 (5m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
