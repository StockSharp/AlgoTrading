# 布林带自动交易策略
[English](README.md) | [Русский](README_ru.md)

该策略结合布林带、RSI 和随机指标，在设定的 GMT 时间窗口内自动开仓。当上一根K线收盘价高于布林带上轨、RSI 大于 75 且随机指标 %K 大于 85 时开空单；当K线收盘价低于布林带下轨、RSI 小于 25 且随机指标 %K 小于 155 时开多单。每个方向只允许一个持仓。策略使用以点数表示的追踪止损来保护持仓。

## 参数

- `OpenBuy` – 是否允许开多单。
- `OpenSell` – 是否允许开空单。
- `GmtTradeStart` – 交易开始小时 (GMT)。
- `GmtTradeStop` – 交易结束小时 (GMT)。
- `BbPeriod` – 布林带周期。
- `RsiPeriod` – RSI 指标周期。
- `StochKPeriod` – 随机指标 %K 周期。
- `StochDPeriod` – 随机指标 %D 周期。
- `StochSlowing` – 随机指标平滑参数。
- `TrailingStop` – 追踪止损距离（点）。
- `CandleType` – 使用的K线周期。
