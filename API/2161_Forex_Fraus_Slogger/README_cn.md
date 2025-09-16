# Forex Fraus Slogger 策略

该策略复现了 MetaTrader 中的包络线反转系统。

## 逻辑

- 计算周期为 1 的 SMA 作为基准价格。
- 上下包络线以 `EnvelopePercent` 的百分比偏离基准。
- 当价格收盘于上轨之上后又回到轨道内时，开空仓。
- 当价格收盘于下轨之下后又回到轨道内时，开多仓。
- 头寸由追踪止损保护。

## 参数

- `EnvelopePercent` – 包络线的百分比偏移（默认 0.1）。
- `TrailingStop` – 追踪止损距离（默认 0.001）。
- `TrailingStep` – 更新追踪止损所需的最小价格移动（默认 0.0001）。
- `ProfitTrailing` – 仅在头寸盈利后启用追踪止损。
- `UseTimeFilter` – 只在指定的时间段交易。
- `StartHour` – 交易时间窗口开始。
- `StopHour` – 交易时间窗口结束。
- `CandleType` – 计算所用的蜡烛周期。

## 说明

- 策略通过 `BuyMarket` 与 `SellMarket` 使用市价单。
- 当价格突破追踪止损价位时，头寸被平仓。
