# Angry Bird Scalping Strategy

该策略使用 StockSharp 高级 API 复现 MetaTrader 平台的 "Angry Bird (Scalping)" 智能交易系统。

## 逻辑
- 观察 15 分钟蜡烛，并在最近 `Depth` 根蜡烛上计算最高价和最低价，用于得到动态网格步长。
- 当没有持仓且上一根蜡烛收盘价高于当前蜡烛时，根据小时级别 RSI 发出信号：RSI 大于 `RsiMin` 开空仓，低于 `RsiMax` 开多仓。
- 如果已有仓位且价格朝不利方向移动至少一个步长，则在同一方向加仓，新仓位的量乘以 `LotExponent`，直到达到 `MaxTrades`。
- 当 CCI 对空头高于 `CciDrop` 或对多头低于 `-CciDrop` 时，立即平掉所有仓位。
- 当收益达到 `TakeProfit` 或亏损达到 `StopLoss`（相对于平均开仓价）时同样平仓。

## 参数
- `StopLoss` – 止损点数。
- `TakeProfit` – 止盈点数。
- `DefaultPips` – 网格最小间距（点）。
- `Depth` – 计算高低点的蜡烛数量。
- `LotExponent` – 加仓量的倍率。
- `MaxTrades` – 最大加仓次数。
- `RsiMin` / `RsiMax` – RSI 入场阈值。
- `CciDrop` – 触发强制平仓的 CCI 绝对值。
- `Volume` – 初始下单量。
- `CandleType` – 使用的蜡烛周期（默认为 15 分钟）。

## 使用方法
将策略连接到某个证券并启动。策略使用市价单管理单一净头寸，当价格不利时会自动加仓。
