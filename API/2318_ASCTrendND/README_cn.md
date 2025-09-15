# ASCTrendND 策略
[Русский](README_ru.md) | [English](README.md)

该策略来源于 MQL5 的 ASCTrendND EA。它使用简单移动平均线作为主要趋势信号，RSI 作为确认过滤器，并以 ATR 乘以倍数作为跟踪止损退出交易。此实现是对 ASCTrend + NRTR + TrendStrength 逻辑在 StockSharp 高级 API 上的简化版本。

## 详情

- **入场条件：**
  - **做多：** 收盘价高于 SMA 且 RSI > 50。
  - **做空：** 收盘价低于 SMA 且 RSI < 50。
- **出场条件：**
  - 基于 ATR * 倍数的跟踪止损或反向信号。
- **止损：** 仅使用 ATR 跟踪止损。
- **默认参数：**
  - `SmaPeriod` = 50
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0
  - `CandleType` = 5 分钟 K 线
- **过滤器：**
  - 类型：趋势跟随
  - 方向：多空皆可
  - 指标：SMA、RSI、ATR
  - 止损：跟踪止损
  - 复杂度：低
  - 时间框架：5m
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
