# Karacatica 策略

## 概述
Karacatica 策略是一种趋势跟随方法，将价格行为与平均趋向指数（ADX）结合使用。当最新收盘价高于或低于 *Period* 根K线之前的收盘价，并且 +DI 或 -DI 领先时产生交易信号。

## 指标
- **Average Directional Index (ADX)** – 衡量趋势强度，同时提供 +DI 与 -DI 线。
- **价格比较** – 比较当前收盘价与 *Period* 根之前的收盘价。

## 参数
- `Period` – 用于ADX计算及价格比较的周期数，默认70。
- `TakeProfitPercent` – 以入场价百分比表示的止盈，默认2%。
- `StopLossPercent` – 以入场价百分比表示的止损，默认1%。
- `CandleType` – 订阅的K线时间框架，默认1小时。

## 交易逻辑
- **做多**：`Close > Close[Period]` 且 `+DI > -DI`，并且不存在先前的做多信号时开多仓，平掉空仓。
- **做空**：`Close < Close[Period]` 且 `-DI > +DI`，并且不存在先前的做空信号时开空仓，平掉多仓。
- **仓位保护**：使用 `StartProtection` 同时应用止盈和止损百分比。

## 使用说明
- 基于 StockSharp 高级 API，订阅K线并绑定 ADX 指标。
- 当出现反向信号时自动平仓并反向建仓。
- 当前仅提供 C# 版本，暂不包含 Python 实现。

## 免责声明
此示例仅用于教育目的，不保证获利。交易具有高风险，请在实盘前充分测试策略。
