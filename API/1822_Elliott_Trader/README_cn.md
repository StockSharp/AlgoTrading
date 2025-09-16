# Elliott Trader 策略

当四小时周期的随机指标达到极值时，该策略会开启分层仓位。它先执行市价单，然后在配置好的点差上方或下方挂出一系列限价单。当达到盈利目标并且趋势由移动平均线和布林带确认后，策略将平仓。

## 入场规则
- 使用随机指标（%K 长度 21，平滑 3）在 H4 K 线。
- 当 %K ≥ **Overbought**：
  - 市价卖出。
  - 在当前价上方按设置的点差挂出最多八个 `SellLimit` 订单。
- 当 %K ≤ **Oversold**：
  - 市价买入。
  - 在当前价下方按设置的点差挂出最多八个 `BuyLimit` 订单。

## 出场规则
- 已实现利润达到 **ProfitTarget** 且价格满足趋势条件时：
  - 多单在价格高于布林带下轨且 200 周期 SMA 高于 55 周期 SMA 时平仓。
  - 空单在价格低于布林带上轨且 200 周期 SMA 低于 55 周期 SMA 时平仓。
- 当 %K ≥ 90 且 200 周期 SMA ≤ 55 周期 SMA 时，取消所有挂出的买入限价单。
- 当 %K ≤ 10 且 200 周期 SMA ≥ 55 周期 SMA 时，取消所有挂出的卖出限价单。

## 参数
- `StochLength` – 随机指标 %K 周期。
- `OverboughtLevel` – 触发做空的超买水平。
- `OversoldLevel` – 触发做多的超卖水平。
- `ProfitTarget` – 平仓所需的已实现利润。
- `Order2Offset` … `Order9Offset` – 额外限价单的点差距离。
- `CandleType` – 使用的时间框架，默认 4 小时。

## 指标
- StochasticOscillator
- BollingerBands
- SMA（200 与 55）
