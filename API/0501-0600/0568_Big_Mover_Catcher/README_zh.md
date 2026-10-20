# Big Mover Catcher

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

使用已完成的一分钟收盘价和布林带（40 根、两倍总体标准差）。空仓时，收盘高于上轨买入 `Volume`，低于下轨卖出 `Volume`。`TradeDirection = null` 允许双向，`Buy` 或 `Sell` 限制开仓。

默认启用全部五个过滤器，且须同时确认：多头 RSI(14) 严格大于 50、空头小于 50；ADX(28) 至少 20；`100 * ATR(14) / Close >= 0.05`；收盘位于 EMA(350) 上方/下方；MACD(12,26,9) 柱值为正/负。各项可独立关闭。只等待启用的指标形成，因此关闭 EMA 后不必等待 350 根。

完整收盘价不高于中轨时平全部多仓，不低于中轨时平全部空仓。启用 `IsForceTakeProfitEnabled` 后，当前蜡烛顺仓方向的实体除以开盘价达到 `ForceTakeProfitPercent` 百分比也退出，默认 5。该条件衡量当根实体而非入场利润，默认关闭。退出优先，同一根不反转。

原生本地市价止损距离由 `StopLossPercent` 给出，默认实际入场的 2%。活动订单防止重复提交。

本地原生保护使用该品种的蜡烛和已订阅价格更新，距离以实际入场成交为基准。退出为市价订单，成交价受可用市场及价格步长影响。

## 参数

| 参数 | 默认值 |
|---|---|
| `BollingerPeriod` | `40` |
| `BollingerDeviation` | `2` |
| `IsRsiFilterEnabled` | `true` |
| `RsiPeriod` | `14` |
| `RsiLongThreshold` | `50` |
| `RsiShortThreshold` | `50` |
| `IsAdxFilterEnabled` | `true` |
| `AdxPeriod` | `28` |
| `MinimumAdx` | `20` |
| `IsAtrFilterEnabled` | `true` |
| `AtrPeriod` | `14` |
| `MinimumAtrPercent` | `0.05` |
| `IsEmaFilterEnabled` | `true` |
| `EmaPeriod` | `350` |
| `IsMacdFilterEnabled` | `true` |
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `TradeDirection` | `null` |
| `StopLossPercent` | `2` |
| `IsForceTakeProfitEnabled` | `false` |
| `ForceTakeProfitPercent` | `5` |
| `CandleType` | `1m` |
