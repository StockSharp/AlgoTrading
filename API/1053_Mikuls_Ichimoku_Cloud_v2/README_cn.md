# Mikul's Ichimoku Cloud v2 策略
[English](README.md) | [Русский](README_ru.md)

基于一目均衡表的突破策略，可选移动平均过滤。仓位通过跟踪止损（ATR、百分比或一目规则）和可选的止盈来管理。

## 细节

- **入场条件**：Tenkan-sen 上穿 Kijun-sen 且价格在云层之上，或价格强势突破绿色云层。
- **多/空**：仅做多。
- **出场条件**：跟踪止损或一目反转，可选止盈。
- **止损**：跟踪。
- **默认值**：
  - `TrailSource` = `LowsHighs`
  - `TrailMethod` = `Atr`
  - `TrailPercent` = 10
  - `SwingLookback` = 7
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1
  - `AddIchiExit` = false
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 25
  - `UseMaFilter` = false
  - `MaType` = `Ema`
  - `MaLength` = 200
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouBPeriod` = 52
  - `Displacement` = 26
  - `CandleType` = TimeSpan.FromHours(1)
- **筛选**：
  - 类别：Trend
  - 方向：Long
  - 指标：Ichimoku, ATR
  - 止损：Trailing
  - 复杂度：Medium
  - 时间框架：Intraday (1h)
  - Seasonality：No
  - Neural Networks：No
  - Divergence：No
  - 风险等级：Medium
