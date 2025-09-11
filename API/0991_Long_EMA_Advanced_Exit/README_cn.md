# Long EMA Advanced Exit
[English](README.md) | [Русский](README_ru.md)

Long EMA Advanced Exit 是一个仅做多策略，当短期均线向上穿越中期均线且价格位于长期均线上方时入场。退出条件包括 MACD 死叉、价格收于选定均线之下、短期均线下穿中期均线、可选的跟踪止损以及基于 ATR 的波动率过滤器。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：短期均线上穿中期均线且价格在长期均线上方。
- **出场条件**：MACD 死叉、价格跌破选定均线、短期均线下穿中期均线、可选跟踪止损。
- **止损**：可选跟踪止损。
- **默认参数**：
  - `MaType` = EMA
  - `EntryConditionType` = Crossover
  - `LongTermPeriod` = 200
  - `ShortTermPeriod` = 5
  - `MidTermPeriod` = 10
  - `EnableMacdExit` = true
  - `MacdCandleType` = TimeSpan.FromDays(7).TimeFrame()
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 15
  - `UseMaCloseExit` = false
  - `MaCloseExitPeriod` = 50
  - `UseMaCrossExit` = true
  - `UseVolatilityFilter` = false
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**：
  - 分类：趋势跟随
  - 方向：仅多头
  - 指标：MA, MACD, ATR
  - 复杂度：中等
  - 风险等级：中等
