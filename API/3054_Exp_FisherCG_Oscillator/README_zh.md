# Exp Fisher CG Oscillator 策略
[English](README.md) | [Русский](README_ru.md)

本策略将 **Exp_FisherCGOscillator** MetaTrader 5 智能交易程序迁移到 StockSharp 高阶 API。它重建 Fisher Center of Gravity 振荡器及其触发线，根据可配置的历史 K 线读取信号，并使用 StockSharp 订单复现原有的止损/止盈流程。

## 工作原理

1. **指标流水线**——每根收盘 K 线都会进入 Fisher CG 振荡器：先对最高价与最低价计算中值，送入中心重力算法；随后在最近 `Length` 根 K 线上进行归一化，并应用 Fisher 变换得到主振荡曲线。`Trigger` 线等于主值向后平移一根 K 线。
2. **信号判断**——策略读取由 `SignalBar` 指定的两根历史数据。当更早的振荡值 (`SignalBar + 1`) 位于触发线之上，同时较新的值 (`SignalBar`) 再次上穿触发线时，判定为多头拐点；空头逻辑完全对称。
3. **离场管理**——一旦较早的振荡值跌破触发线就立即平掉多单；若其上破触发线则平掉空单，对应原始 EA 中的 `BUY_Close` / `SELL_Close` 标志。若出现反向信号，策略会先平仓再考虑反向开仓。
4. **逐根收盘处理**——所有计算都基于 `CandleType` 的收盘 K 线完成，避免盘中噪音，同时契合 EA 中的“新 K 线”判断。

## 风险管理与仓位控制

- **止损/止盈**——`StopLossPoints` 与 `TakeProfitPoints` 以价格步长表示，会通过 `Security.PriceStep` 换算成绝对价格距离。
- **仓位模式**——`SizingMode = FixedVolume` 时直接使用固定手数 `FixedVolume`；`SizingMode = PortfolioShare` 则按照组合当前价值的 `DepositShare` 比例，结合最新收盘价与 `VolumeStep` 计算手数。
- **单一持仓**——策略始终在开反向仓位前平掉原有仓位，避免同向对冲。

## 参数

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 订阅并计算指标所使用的 K 线周期。 |
| `Length` | Fisher CG 振荡器的周期，也是归一化窗口长度。 |
| `SignalBar` | 读取信号时向前回看的已收盘 K 线数量，`1` 即原脚本默认值。 |
| `AllowLongEntry` / `AllowShortEntry` | 是否允许开多/开空。 |
| `AllowLongExit` / `AllowShortExit` | 是否允许在相反信号下平多/平空。 |
| `StopLossPoints` / `TakeProfitPoints` | 以价格步长表示的止损、止盈距离，设为 `0` 可关闭。 |
| `FixedVolume` | 固定仓位模式下的下单手数。 |
| `DepositShare` | 在 `PortfolioShare` 模式下，每次交易使用的资产比例。 |
| `SizingMode` | 在固定手数与按资金比例之间切换。 |

## 使用提示

- 请将 `CandleType` 与 `SignalBar` 设置为与原 EA 相同的周期与偏移（默认 8 小时周期，偏移 1）。
- 指标需要一定历史数据才能形成，初始化阶段不会触发交易。
- 止损与止盈基于 K 线收盘价触发，请根据品种的最小报价单位调节点数参数。
- 若选择 `PortfolioShare` 模式，请确保组合估值可用；否则策略会退回到固定手数模式。

## 与原 EA 的差异

- 所有交易均以市价单执行，不再使用 `Deviation_` 滑点参数；滑点由 StockSharp 自行处理。
- 资金管理简化为 `FixedVolume` 与 `PortfolioShare` 两种模式，原脚本的亏损份额分配选项未实现。
- 不再使用 `UpSignalTime` / `DnSignalTime` 挂单时间，信号在当前收盘 K 线处理完成后立即执行。
