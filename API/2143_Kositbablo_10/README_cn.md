# Kositbablo 10
[English](README.md) | [Русский](README_ru.md)

多时间框架的 EURUSD 策略，基于 RSI 和 EMA 信号。
策略同时检查日线和小时线指标，当趋势条件一致时在市场价开仓。

## 参数
- **Take Profit**：以点数表示的止盈。
- **Stop Loss**：以点数表示的止损。
- **Turbo Mode**：在已有仓位时仍允许开新仓。

## 规则
- 当日线 RSI(11) < 60、小时线 RSI(5) < 48 且 EMA20 > EMA2 时做多。
- 当日线 RSI(22) > 38、小时线 RSI(20) > 60 且 EMA23 > EMA12 时做空。
- 仅在小时线收盘后进行交易。
