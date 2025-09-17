# Exp TEMA 策略

**Exp TEMA** 策略是 MetaTrader 专家顾问 `Exp_TEMA.mq5` 的 StockSharp 版本。原始 EA 会同时监控多只外汇货币对，通过 Triple Exponential Moving Average (TEMA) 的斜率方向来决定开仓与平仓。本次移植保持相同的指标逻辑，不过在 StockSharp 中一次聚焦于一个指定的证券。

## 交易逻辑

策略只处理 `CandleType` 参数产生的收盘 K 线。在每根 K 线结束时计算周期为 `TemaPeriod` 的 TEMA，并使用最近三根的指标值重建原版的斜率判断：

1. `tema[0]` 为当前 K 线值，`tema[1]` 为上一根，`tema[2]` 为前两根。
2. 最近的斜率为 `d1 = tema[1] - tema[2]`，较旧的斜率为 `d2 = tema[2] - tema[3]`。
3. 当斜率向上翻转时触发 **做多** (`d2 < 0` 且 `d1 > 0`)。先平掉空头，再以 `Volume + |Position|` 的数量市价买入。
4. 当斜率向下翻转时触发 **做空** (`d2 > 0` 且 `d1 < 0`)。先平掉多头，再以 `Volume + |Position|` 的数量市价卖出。
5. 保护性退出遵循原始 EA 的规则：若当前斜率为负，则平掉多头；若当前斜率为正，则平掉空头。

因此，策略在不访问原始缓冲区的情况下复现了相同的信号时机，并完全符合 StockSharp 的高层 API 要求。

## 参数说明

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TemaPeriod` | 15 | Triple Exponential Moving Average 的计算周期。 |
| `TradeVolume` | 1 | 基础下单手数。反手时实际下单量为 `TradeVolume + |Position|`。 |
| `StopLossPoints` | 1000 | 止损距离（价格步长数量）。若大于零则传入 `StartProtection`。 |
| `TakeProfitPoints` | 2000 | 止盈距离（价格步长数量）。若大于零则传入 `StartProtection`。 |
| `CandleType` | 15 分钟 | 用于计算指标的 K 线类型，请选择与原始 EA 图表一致的周期。 |

所有参数均通过 `StrategyParam<T>` 创建，可在 Designer 中进行优化。

## 与 MQL5 版本的差异

- 原版 EA 可以一次处理最多 12 个品种。StockSharp 策略绑定到单一 `Security`，因此此移植版一次交易一个品种。若需要多品种，可同时运行多个策略实例。
- 下单与风控使用 `BuyMarket`、`SellMarket` 以及 `StartProtection`，对应原策略的市价单、止损与止盈，但实现方式遵循 StockSharp 高层 API。
- 指标数据通过 `SubscribeCandles().Bind(...)` 获取，无需手动复制缓冲区，符合仓库的编码规范。

## 使用建议

1. 为策略指定目标证券，并将 `CandleType` 调整为与分析时间框架一致。
2. 根据品种波动性调整 `StopLossPoints` 与 `TakeProfitPoints` 的步长距离。
3. 可选地对 `TemaPeriod`、`StopLossPoints`、`TakeProfitPoints` 进行参数优化，以复现 MetaTrader 中的调参流程。
4. 利用自动创建的图表区域观察价格、TEMA 曲线以及实际成交。
