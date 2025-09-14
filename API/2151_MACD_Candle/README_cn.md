# MACD Candle 策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MetaTrader 智能交易系统“Exp_MACDCandle”的改写版本。它将 MACD Candle 指标的颜色信号转换为使用 StockSharp 高级 API 的交易指令。

## 概念

MACD Candle 指标基于开盘价和收盘价计算的 MACD 值构建合成蜡烛。如果收盘的 MACD 高于开盘的 MACD，则蜡烛为多头（颜色 2）；反之为空头（颜色 0）；两者相等时为中性（颜色 1）。

当出现多头蜡烛并且前一根蜡烛不是多头时，策略做多；当出现空头蜡烛并且前一根蜡烛不是空头时，策略做空。已有仓位会按新方向反转。

## 参数

- `FastLength` – MACD 快速 EMA 周期，默认 12。
- `SlowLength` – MACD 慢速 EMA 周期，默认 26。
- `SignalLength` – MACD 信号线周期，默认 9。
- `CandleType` – 计算所用的蜡烛类型，默认四小时 `TimeFrameCandle`。

所有参数均可配置并支持优化。

## 入场与出场规则

- **做多**：收盘 MACD 高于开盘 MACD，且上一根蜡烛不是多头。
- **做空**：开盘 MACD 高于收盘 MACD，且上一根蜡烛不是空头。
- **出场**：出现相反信号时平仓；不设置止损或止盈。

## 说明

- 策略使用市价单（`BuyMarket` 和 `SellMarket`）。
- 仅在蜡烛收盘后评估信号，以减少噪音。
- 示例仅供学习，不包含风险管理。
