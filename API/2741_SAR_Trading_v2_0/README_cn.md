# SAR Trading v2.0 策略

**SAR Trading v2.0** 将 Cronex 的经典 MQL5 策略迁移到 StockSharp 高阶 API。策略结合简单移动平均线 (SMA) 与 Parabolic SAR，在出现方向信号时入场，并通过固定止损、止盈以及基于点差的跟踪止损管理持仓。

- 指标：Simple Moving Average、Parabolic SAR。
- 默认周期：15 分钟 K 线（可通过 `CandleType` 调整）。
- 适用市场：任意提供有效 `PriceStep`（最小报价步长）的品种。

## 交易逻辑
- 只有在仓位为空时才会评估入场信号。
- **做多：** 当 Parabolic SAR 跌破 SMA，或 `MaShift` 根之前的收盘价低于 SMA 时入场，多头条件对应原始代码的 `SAR < MA || Close[shift] < MA` 判断。
- **做空：** 当 Parabolic SAR 升至 SMA 之上，或 `MaShift` 根之前的收盘价高于 SMA 时入场。
- 发出离场委托后，策略会等待仓位归零，再处理新的信号，从而保持一次仅持有一个方向的仓位，与原 EA 一致。

## 风险控制
- `StopLossPips` 与 `TakeProfitPips` 使用 `Security.PriceStep` 将点差转换为绝对价格距离。
- `TrailingStopPips` 在持仓盈利后，将保护性止损保持在固定的点数距离。
- `TrailingStepPips` 要求价格额外向有利方向运行指定点数后，才会再次移动跟踪止损，完整复刻 MQL 中的“步进”机制。
- 一旦触发止损或止盈，策略将以市价方式平仓。

## 参数说明
- `MaPeriod`（默认 **18**）：SMA 的计算周期。
- `MaShift`（默认 **2**）：与 SMA 对比时向前回看的收盘价数量。
- `SarStep`（默认 **0.02**）：Parabolic SAR 的初始加速因子。
- `SarMaxStep`（默认 **0.2**）：Parabolic SAR 的最大加速因子。
- `StopLossPips`（默认 **50**）：固定止损距离，单位为点。
- `TakeProfitPips`（默认 **50**）：固定止盈距离，单位为点。
- `TrailingStopPips`（默认 **15**）：跟踪止损与价格的间距，单位为点。
- `TrailingStepPips`（默认 **5**）：每次移动跟踪止损前所需的额外盈利点数。
- `CandleType`：用于计算的 K 线类型。

## 其他说明
- 策略在内部保存收盘价历史，以复现 MQL 中的 `iClose(shift)` 调用。
- 所有决策均基于已完成的 K 线，确保与原始专家顾问的行为一致。
- 下单数量来源于策略的 `Volume` 属性；默认每个信号下单 1 手。

