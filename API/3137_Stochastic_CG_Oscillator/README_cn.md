# 随机CG振荡器策略
[English](README.md) | [Русский](README_ru.md)

该策略将 **Exp_StochasticCGOscillator** MetaTrader 5 智能交易系统移植到 StockSharp。转换过程完整重建了随机重心振荡器及其触发线平滑算法，并使用 StockSharp 的高级策略 API 执行交易。

## 工作原理

1. **指标流水线** – 所有来自 `CandleType` 的已完成 K 线都会传入自定义的随机 CG 振荡器。策略先计算高低价的中值，再通过长度为 `Length` 的重心循环求出 CG 值，随后使用加权滑动窗口生成振荡器主线。触发线采用 EA 中同样的 `0.96 * (previous + 0.02)` 平滑方式复现。
2. **信号采样** – 策略读取由 `SignalBar` 指定的两个历史数值。当较早的数值（偏移 `SignalBar + 1`）高于触发线且较新的数值（偏移 `SignalBar`）重新跌破触发线时，准备做多信号；做空条件完全对称。
3. **仓位管理** – 只要较早的读数跌破触发线，多头仓位立即平仓；当较早读数升破触发线时，空头仓位被关闭。出现反向入场信号时，会先平掉当前仓位再执行反手订单。
4. **风险控制** – `StopLossPoints` 与 `TakeProfitPoints` 以合约最小跳动值表示，在每根处理完成的 K 线收盘价上检查是否触发，复刻 EA 的止损/止盈设置且无需挂单。
5. **预热流程** – 指标只有在重心循环与四值平滑缓冲区全部就绪后才输出信号，确保回测结果稳定可复现。

## 风险管理与仓位规模

- **止损/止盈** – `StopLossPoints` 与 `TakeProfitPoints` 通过 `Security.PriceStep` 转换为绝对价格距离，设置为 `0` 时表示禁用。
- **单向持仓** – 策略从不同时持有多头和空头。若出现反向信号，会先执行平仓，再根据新方向建仓。
- **仓位规模** – `SizingMode = FixedVolume` 时始终使用 `FixedVolume`。`SizingMode = PortfolioShare` 则按照最新收盘价和 `Security.VolumeStep` 将账户市值中 `DepositShare` 的比例换算成合约数量。

## 参数

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 订阅并计算指标所用的 K 线周期。 |
| `Length` | 随机 CG 振荡器的周期，同时决定 CG 与归一化窗口长度。 |
| `SignalBar` | 回溯多少根已完成 K 线来评估信号（`1` 对应 EA 默认值）。 |
| `AllowLongEntry` / `AllowShortEntry` | 控制是否允许开多/开空。 |
| `AllowLongExit` / `AllowShortExit` | 控制是否自动平多/平空。 |
| `StopLossPoints` / `TakeProfitPoints` | 按价格步长表示的止损与止盈距离，设置为 `0` 即关闭。 |
| `FixedVolume` | 固定仓位模式下发送的订单手数。 |
| `DepositShare` | 组合市值中用于仓位换算的比例。 |
| `SizingMode` | 选择固定手数或按市值比例换算仓位。 |

## 使用提示

- 建议将 `CandleType` 设置为 EA 所用的周期（原版为 8 小时）。较大的 `SignalBar` 需要更长的预热时间，以便指标历史缓冲区覆盖相应的偏移。
- 止损和止盈基于收盘价触发，并不会在 K 线内部挂单；请根据标的的最小跳动调整点数。
- 若启用 `PortfolioShare` 模式，请确保组合估值可用；若无法获取，策略会回退到固定手数。
- 指标输出范围保持在 `[-1, 1]`，与原实现一致，可继续叠加熟悉的阈值过滤或辅助条件。

## 与原始 EA 的差异

- 策略直接发送市价单，不再使用 `Deviation_` 参数控制滑点；成交滑点交由 StockSharp 执行层处理。
- 资金管理精简为两种模式（固定手数与按市值比例），未实现 EA 中基于保证金的其他选项。
- EA 中的挂单时间戳（`UpSignalTime` / `DnSignalTime`）在本实现中不再需要，因为策略仅处理已完成的 K 线并同步执行。
