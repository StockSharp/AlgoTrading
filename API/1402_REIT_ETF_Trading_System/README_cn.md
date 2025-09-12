# REIT ETF Trading System 策略
[English](README.md) | [Русский](README_ru.md)

该策略针对 REIT ETF 的周线图，结合布林带突破和唐奇安通道趋势信号，并使用国债收益率及与 SPY、TNX 的相关性作为过滤器。

## 细节

- **入场条件**：
  - 布林带突破，带有收益率和相关性过滤。
  - 唐奇安通道突破，附加收益率或相关性或 TNX 趋势过滤。
- **多空方向**：仅做多。
- **出场条件**：
  - 基于 TNX 的移动止损。
  - 超买与止损条件。
- **止损**：ATR 追踪止损与百分比止损。
- **默认参数**：
  - `BollingerLength` = 15
  - `BollingerMultiplier` = 2
  - `TnxLookbackPeriod` = 25
  - `TnxMinChangePercent` = 15
  - `DonchianChannelLength` = 30
  - `MaxCorrelationForBuy` = 0.3
  - `MinYield` = 2
  - `AtrStopMultiplier` = 1.5
  - `StopLossPercent` = 8
- **过滤器**：
  - 类别：趋势跟随
  - 方向：多
  - 指标：Bollinger Bands、Donchian Channel、ATR、Stochastic、Correlation
  - 止损：有
  - 复杂度：中
  - 时间框架：周线
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
