# Safa Bot Alert 策略
[English](README.md) | [Русский](README_ru.md)

Safa Bot Alert 策略利用短期 SMA 和 ADX 过滤器进行趋势交叉交易。价格上穿 SMA 且 ADX 高于阈值时做多，下穿且 ADX 高于阈值时做空。策略使用固定止盈、止损和跟踪止损，并在指定的会话时间平仓所有仓位。

## 细节

- **入场条件**：价格与 SMA 交叉且 ADX > `AdxThreshold`。
- **多/空**：双向。
- **出场条件**：止盈、止损、跟踪止损或会话收盘。
- **止损**：固定和跟踪。
- **默认值**：
  - `SmaLength` = 3
  - `TakeProfitPoints` = 80m
  - `StopLossPoints` = 35m
  - `TrailPoints` = 15m
  - `AdxLength` = 15
  - `AdxThreshold` = 15m
  - `SessionCloseHour` = 16
  - `SessionCloseMinute` = 0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：SMA, ADX
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：否
  - 背离：否
  - 风险级别：中
