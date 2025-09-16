# Ichimoku Chinkou Cross 策略

该策略基于 Ichimoku 的 Chinkou Span（滞后线）与价格的交叉进行交易。

## 策略逻辑

- **做多：** Chinkou 从下方上穿价格，且当前价格和 Chinkou 都在 Kumo 云之上，同时 RSI 高于 `RsiBuyLevel`。
- **做空：** Chinkou 从上方下穿价格，且当前价格和 Chinkou 都在 Kumo 云之下，同时 RSI 低于 `RsiSellLevel`。

策略通过 `StartProtection` 设置止损，并包含 Tenkan、Kijun、Senkou Span B 和 RSI 的参数。

## 参数

| 名称 | 描述 | 默认值 |
|------|------|-------|
| `TenkanPeriod` | Tenkan-sen 周期 | 9 |
| `KijunPeriod` | Kijun-sen 周期 | 26 |
| `SenkouSpanPeriod` | Senkou Span B 周期 | 52 |
| `RsiPeriod` | RSI 计算周期 | 14 |
| `RsiBuyLevel` | 做多所需的最小 RSI | 70 |
| `RsiSellLevel` | 做空所需的最大 RSI | 30 |
| `StopLoss` | 止损百分比或数值 | 2% |
| `CandleType` | 订阅蜡烛类型 | 5 分钟蜡烛 |

## 指标

- Ichimoku
- RSI
