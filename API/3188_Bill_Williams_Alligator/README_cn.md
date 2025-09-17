# Bill Williams Alligator 策略

本策略将 MetaTrader 5 专家顾问 **“Bill Williams.mq5”**（作者 Vladimir Karputov）完整移植到 StockSharp 高层 API。系统订阅单一 K 线序列，重建 Bill Williams 分形，并将其与向前平移的 Alligator 三条线（下颌、牙齿、嘴唇）进行比较。当最新完成的 K 线收盘价突破最近的向上或向下分形，且该分形位于所有 Alligator 线之外时，策略会开仓。额外的风控参数覆盖原始 EA 中的设置，包括止损、止盈、移动止损、信号反向以及强制平掉相反仓位。

## 交易逻辑

1. **分形检测**：每根完成的 K 线都会更新最高价和最低价的滚动缓冲区，最多回溯 `FractalsLookback` 根已完成的 K 线，寻找最近一次确认的向上/向下分形（五根 K 线模式）。
2. **Alligator 复现**：用 `(High + Low) / 2` 的中间价驱动三条 `SmoothedMovingAverage` 指标，分别代表下颌、牙齿和嘴唇，并按照参数要求向前平移指定的 K 线数量，与 MetaTrader 的绘制方式保持一致。
3. **突破确认**：做多信号要求最新向上分形高于所有三条 Alligator 线，并且当前 K 线收盘价高于该分形。做空逻辑相反，需要价格跌破向下分形且分形位于三条线之下。
4. **下单执行**：默认情况下，当出现突破并且没有持仓时，策略以 `OrderVolume` 的仓位开市价单。如果启用 `CloseOppositePositions`，会在开新仓前先平掉反向仓位。将 `ReverseSignals` 设为 `true` 可实现原始 EA 的信号反向模式。
5. **风险管理**：止损和止盈价格在内部跟踪，并在每根 K 线上进行验证。移动止损在浮动盈利达到 `TrailingStopPips + TrailingStepPips` 后启动，并随着价格推进按步进调整。所有距离都使用根据交易标的 `PriceStep` 计算的“点数”，自动兼容 MetaTrader 常见的 3/5 位小数报价。

## 参数

| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `OrderVolume` | 入场手数（或合约数）。 | `0.1` |
| `StopLossPips` | 初始止损距离（点）。设为 `0` 表示不使用止损。 | `50` |
| `TakeProfitPips` | 止盈距离（点）。设为 `0` 表示不使用止盈。 | `50` |
| `TrailingStopPips` | 移动止损距离（点）。`0` 表示禁用移动止损。 | `10` |
| `TrailingStepPips` | 每次移动止损前需要额外获得的点数；启用移动止损时必须为正。 | `5` |
| `JawPeriod` | Alligator 下颌线的平滑移动平均周期。 | `13` |
| `JawShift` | 下颌线向前平移的 K 线数。 | `8` |
| `TeethPeriod` | Alligator 牙齿线的平滑移动平均周期。 | `8` |
| `TeethShift` | 牙齿线向前平移的 K 线数。 | `5` |
| `LipsPeriod` | Alligator 嘴唇线的平滑移动平均周期。 | `5` |
| `LipsShift` | 嘴唇线向前平移的 K 线数。 | `3` |
| `FractalsLookback` | 回溯的完成 K 线数量，用于寻找最近的分形。 | `100` |
| `ReverseSignals` | `true` 时，向下分形触发买入，向上分形触发卖出。 | `false` |
| `CloseOppositePositions` | `true` 时，在开新仓前先平掉反向仓位。 | `false` |
| `CandleType` | 用于计算和生成信号的 K 线类型。 | `TimeFrame(1h)` |

## 说明

- 策略仅处理 **已完成的 K 线**，忽略盘中 tick，完全遵循原始 EA 的逐 K 线逻辑。
- 为了兼容 MetaTrader 的 3/5 位报价，若 `Security.Decimals` 为 3 或 5，则将 `PriceStep` 乘以 10 以得到“点”大小。
- 止损、止盈和移动止损均由策略内部管理；若下一根 K 线触及触发价，则直接通过市价单平仓。
- 如果图表区域可用，策略会自动绘制 K 线和 Alligator 指标，方便与 MetaTrader 模板对比。
- 根据仓库约定，本次转换不包含 Python 版本和测试项目。
