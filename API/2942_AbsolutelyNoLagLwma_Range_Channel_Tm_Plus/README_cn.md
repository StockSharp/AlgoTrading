# AbsolutelyNoLagLWMA Range Channel TM Plus 策略

## 概述
该策略为 MetaTrader 专家顾问“Exp_AbsolutelyNoLagLwma_Range_Channel_Tm_Plus”的 StockSharp 版本。它使用对最高价和最低价进行双重线性加权移动平均（LWMA）平滑得到的价格通道，并在通道突破时执行交易。移植过程中保留了原有行为：仅在所选周期的已完成 K 线触发信号、通道状态与原指标完全一致、仓位管理按照“时间退出 → 指标退出 → 新开仓”的顺序执行。

## 指标构建
1. 每根完成的 K 线的最高价与最低价分别送入第一层 LWMA，长度由 `Length` 参数控制。
2. 第一层 LWMA 的输出再次经过同样长度的第二层 LWMA，以复现“AbsolutelyNoLagLWMA”指标的平滑方式。
3. 将最终的上轨与下轨与 K 线收盘价比较：
   * 收盘价高于上轨 → 看涨突破状态；
   * 收盘价低于下轨 → 看跌突破状态；
   * 收盘价位于通道内部 → 中性状态。
4. 策略会保存最近若干个通道状态。`SignalBar` 参数决定使用哪一根历史 K 线来评估信号（0 表示最近一根已收线，与原始 MQL 参数含义一致）。

## 信号解释
* **多头开仓**：由 `EnableBuyEntries` 控制。策略在 `SignalBar + 1` 指定的 K 线上检测到看涨突破，同时 `SignalBar` 对应的 K 线已经返回到通道内部时开多，与原程序的“上一根突破”逻辑完全一致。
* **空头开仓**：由 `EnableSellEntries` 控制，逻辑与多头对称。
* **多头平仓**：由 `EnableBuyExits` 控制。当参考 K 线出现看跌突破且当前 K 线未因时间退出提前平仓时，策略会关闭现有多单。
* **空头平仓**：由 `EnableSellExits` 控制。当参考 K 线出现看涨突破且当前 K 线未触发时间退出时，策略会关闭现有空单。

## 仓位管理
* **下单数量**：来自 `OrderVolume` 参数。若需要反手，系统会自动补足当前仓位的绝对值，避免残留头寸。
* **止损 / 止盈**：`StopLossPoints` 与 `TakeProfitPoints` 以合约最小变动单位表示。当值大于 0 时会乘以合约 `PriceStep` 转换为绝对价格，并通过 `StartProtection` 启用保护单；为 0 时表示关闭该功能。
* **持仓时间控制**：原 EA 中的 `TimeTrade`/`nTime` 机制通过 `UseTimeExit` 和 `HoldingLimit` 复现。每根完成的 K 线都会优先检查是否超出持仓时限，若超出则立即发出平仓指令。
* **持仓时间戳**：策略记录最近一次导致多头或空头仓位的成交时间，用于计算持仓时长。

## 参数说明
| 参数 | 说明 |
|------|------|
| `Length` | 两层 LWMA 的共同长度，用于构建价格通道。 |
| `SignalBar` | 用于检测信号的 K 线位移（0 表示最近一根已收线）。 |
| `CandleType` | 供指标与交易逻辑使用的时间周期。 |
| `OrderVolume` | 新开仓订单的数量。 |
| `StopLossPoints` | 止损距离（以最小变动单位计），0 表示不启用。 |
| `TakeProfitPoints` | 止盈距离（以最小变动单位计），0 表示不启用。 |
| `EnableBuyEntries` | 是否允许开多。 |
| `EnableSellEntries` | 是否允许开空。 |
| `EnableBuyExits` | 是否允许指标信号平多。 |
| `EnableSellExits` | 是否允许指标信号平空。 |
| `UseTimeExit` | 是否启用基于持仓时间的自动平仓。 |
| `HoldingLimit` | 持仓时限，超过后将触发时间退出。 |

## 使用提示
* 通道完全依据最高价与最低价计算，等价于 MQL 中的 `AbsolutelyNoLagLwma_Range_Channel` 指标。
* 策略只处理已完成的 K 线，避免在形成过程中的数据造成虚假信号。
* 将 `SignalBar` 设为 0 可复现常见的 MT5 配置；默认值 1 对应原专家顾问的“延迟一根 K 线确认”逻辑。
* 若品种缺少 `PriceStep` 定义，则止损与止盈会被自动忽略，行为与原脚本中的 0 值输入一致。
