# ADX System
[English](README.md) | [Русский](README_ru.md)

**ADX System** 策略使用平均方向指数 (ADX) 及其 +DI 和 -DI 线进行交易。当 ADX 上升且某一方向线突破 ADX 时开仓。策略设定固定的止盈和止损，并使用追踪止损来保护盈利。

## 细节

- **入场条件**
  - ADX 上升（前一值低于当前值）。
  - **多头**：前一 +DI 低于前一 ADX，且当前 +DI 高于当前 ADX。
  - **空头**：前一 -DI 低于前一 ADX，且当前 -DI 高于当前 ADX。
- **出场条件**
  - ADX 与 DI 线给出相反信号。
  - 价格触及追踪止损。
  - 价格达到固定的止盈或止损。
- **方向**：双向。
- **止损/止盈**：以绝对价位设置固定止损、止盈和追踪止损。
- **默认值**：
  - `AdxPeriod` = 14
  - `TakeProfit` = 15
  - `StopLoss` = 100
  - `TrailingStop` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选器**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：ADX、+DI、-DI
  - 止损：有
  - 复杂度：初级
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等

