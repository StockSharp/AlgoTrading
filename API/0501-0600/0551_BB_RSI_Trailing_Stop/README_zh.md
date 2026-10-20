# BB RSI Trailing Stop

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

布林带突破后必须出现 RSI 从极端区域恢复，随后使用有条件的移动止损。`p` 为前一根指标已形成的蜡烛；RSI 使用 Wilder 平滑。

## 交易规则

做多：原始 `Low < lowerBand` 且 `RSI[p] <= RsiOversold && RSI > RsiOversold`。做空：原始 `High > upperBand` 且 `RSI[p] >= RsiOverbought && RSI < RsiOverbought`。仅处于超卖或超买区并不足够。仅空仓时以 `Volume` 入场，无隐藏等待期。

以加权实际入场价为基准，多头使用可执行 bid，空头使用 ask。初始距离为 `StopLossPoints`；有利报价极值移动达到 `TrailOffsetPoints` 后才启用跟踪。此后止损距该极值 `TrailStopPoints`，且不得放宽初始止损。报价触及止损时以市价平掉全部实际持仓。三种距离均为绝对价格单位，不是跳动点或百分比；不使用较早的蜡烛极值触发稍后才创建的止损。

仅在指标充分预热后使用已完成的蜡烛生成入场信号。持仓持续管理至退出；未完成的市价订单会阻止重复提交。

布林带采用滚动收盘价窗口的总体标准差，包含当前已完成蜡烛。

## 参数

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `25` |
| `BollingerDeviation` | `2` |
| `RsiPeriod` | `14` |
| `RsiOversold` | `33` |
| `RsiOverbought` | `60` |
| `StopLossPoints` | `50` |
| `TrailOffsetPoints` | `99` |
| `TrailStopPoints` | `40` |
| `CandleType` | `15m` |
