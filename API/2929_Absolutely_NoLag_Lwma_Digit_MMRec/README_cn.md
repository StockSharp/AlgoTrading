# AbsolutelyNoLagLWMA Digit MMRec 策略

## 概述

本策略是 MetaTrader 顾问 *Exp_AbsolutelyNoLagLwma_Digit_NN3_MMRec* 的 StockSharp 版本。保留了原始的多周期结构（12 小时、4 小时和 2 小时），并实现了同样的 `MMRec` 资金管理逻辑。三个模块（A/B/C）分别独立管理自己的仓位份额，策略聚合所有模块的净仓位。

每个模块都会在所选价格（收盘价、开盘价、典型价等）上运行两层加权移动平均（先 WMA 再对结果再次 WMA），从而复制 "AbsolutelyNoLagLWMA" 指标。平滑结果会按照指定的小数位数四舍五入。只要平滑线的斜率发生改变（值开始上升或下降），模块就会触发方向切换并执行交易操作。

## 交易逻辑

1. 等待模块时间框架的蜡烛收盘。
2. 根据 `AppliedPrices` 参数选取价格。
3. 对价格运行主 WMA，再将输出传给第二个 WMA，得到双重平滑值。
4. 将结果四舍五入后与上一值比较。
5. **斜率向上 (`value > previous`)**：
   - 如果允许平掉空头，关闭该模块的空头仓位份额。
   - 若允许做多且当前没有多头仓位，则按当前模块的下单量开多。
   - 根据价格步长更新多头的止损与止盈价格。
6. **斜率向下 (`value < previous`)**：
   - 如果允许平掉多头，关闭该模块的多头仓位份额。
   - 若允许做空且当前没有空头仓位，则开空单。
   - 更新空头的保护价格。
7. 每根蜡烛都会检测最高/最低价是否触发当前的止损或止盈。如果被触发，仓位按照触发价平仓，并将交易结果记录到资金管理队列中。
8. 资金管理会保存最近 *N* 笔（`N` 等于触发参数）交易的结果。如果最近 *N* 笔全部亏损，则下一单使用 `SmallVolume`，否则使用 `NormalVolume`。亏损/盈利的判断基于开仓时保存的价格和实际的平仓价格（止损/止盈/信号平仓）。

策略使用市价单进出场：信号触发的订单假定按蜡烛收盘价成交，止损/止盈假定按对应的保护价成交。

## 参数说明

所有模块拥有相同的参数集合，默认值与原始 MQL 策略一致。

| 参数 | 说明 |
|------|------|
| `ACandleType` / `BCandleType` / `CCandleType` | 模块使用的蜡烛周期（默认 12 小时 / 4 小时 / 2 小时）。 |
| `ALength` / `BLength` / `CLength` | AbsolutelyNoLagLWMA 的平滑长度（对两个 WMA 均生效）。 |
| `AAppliedPrice` / `BAppliedPrice` / `CAppliedPrice` | 指标所用价格类型（收盘价、开盘价、最高价、最低价、中间价、典型价、加权价、简单价、四分位价、TrendFollow1、TrendFollow2、DeMark）。 |
| `ADigits` / `BDigits` / `CDigits` | 平滑值四舍五入时的小数位数。 |
| `ABuyOpen`、`ASellOpen`、`ABuyClose`、`ASellClose`（及 B/C 模块同名参数） | 控制模块是否允许开仓/平仓多头或空头。 |
| `ASmallVolume`、`ANormalVolume` | 减少后的下单量与常规下单量（对多空共用）。 |
| `ABuyLossTrigger`、`ASellLossTrigger` | 触发减少仓位的连续亏损次数（分别对应多头与空头）。 |
| `AStopLossPoints`、`ATakeProfitPoints` | 以价格步长为单位的止损与止盈距离。模块 B/C 拥有同样的参数。 |

当某个触发参数为 0 时，对应方向的亏损队列会被清空。价格步长由 `Security.Step` 获取，如若未设置，则退化为 `1`。

## 实现细节

- 模块各自维护独立的仓位数量，因此不同模块可能出现多空同时存在的情况；策略的净仓位是三个模块仓位的和。
- 止损与止盈在每根完成的蜡烛上使用高/低价进行检查。
- `AppliedPrices` 枚举与原始指标完全一致，包含 TrendFollow 与 DeMark 版本。
- 指标实例通过 `Bind` 管理，并未添加到策略的公共集合，符合仓库的编码规范。
- 只有当趋势方向发生变化时才会开仓或平仓，避免在连续的相同信号上重复下单。
