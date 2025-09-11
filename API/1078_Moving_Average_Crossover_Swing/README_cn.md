# Moving Average Crossover Swing 策略
[English](README.md) | [Русский](README_ru.md)

该策略在快速指数移动平均线上穿或下穿中速均线时交易，可选择使用慢速均线和 MACD 直方图确认。利用 ATR 设定止损和止盈，并可在另一组均线交叉时离场。

## 详情

- **入场条件**：
  - 快速 EMA 上穿/下穿 中速 EMA。
  - 可选：价格高于/低于慢速 EMA。
  - 可选：MACD 直方图高于/低于零。
- **多空方向**：可配置。
- **出场条件**：基于 ATR 的止损/止盈或可选的均线交叉。
- **止损**：是，ATR 倍数。
- **默认值**：
  - `FastPeriod` = 5
  - `MediumPeriod` = 10
  - `SlowPeriod` = 50
  - `FastExitPeriod` = 5
  - `MediumExitPeriod` = 10
  - `AtrPeriod` = 14
  - `AtrStopMultiplier` = 1.4
  - `AtrTakeMultiplier` = 3.2
  - `EnableSlow` = true
  - `EnableMacd` = true
  - `EnableLong` = true
  - `EnableShort` = false
  - `EnableCrossExit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：Trend following
  - 方向：可配置
  - 指标：EMA, MACD, ATR
  - 止损：是
  - 复杂度：中等
  - 时间框架：1m（默认）
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
