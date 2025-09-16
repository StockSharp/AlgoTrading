# MA Cross Method PriceMode 策略

## 概述
**MA Cross Method PriceMode** 策略是 MetaTrader 4 专家顾问“MA_cross_Method_PriceMode”的 StockSharp 移植版。策略比较两条可配置的移动平均线，只要快速均线穿越慢速均线就会触发交易。两条均线均保留了原始 MT4 输入：周期、平滑方法（SMA、EMA、SMMA、LWMA）、价格类型（收盘、开盘、最高、最低、中值、典型价、加权价）以及水平位移。策略可用于任何提供常规时间周期 K 线的标的。

## 指标
- **快速移动平均线** – 可配置周期、方法和价格类型。通过缓存已完成的指标值并在 `FirstShift` 根柱子前读取，复现了 MetaTrader 的水平位移参数。
- **慢速移动平均线** – 同样可配置的周期、方法和价格类型，并使用相同的缓存机制模拟位移。

## 交易逻辑
1. 订阅所选的 K 线类型，并且只处理已经收盘的 K 线，避免在未完成的柱子上重绘。
2. 每根收盘柱分别把对应的价格馈送进两条移动平均线。
3. 当两条均线都返回最终值后，策略评估两个条件：
   - **看多交叉** – 前一根柱子上快速均线小于或等于慢速均线，而当前柱子上快速均线向上穿越慢速均线。
   - **看空交叉** – 前一根柱子上快速均线大于或等于慢速均线，而当前柱子上快速均线向下穿越慢速均线。
4. 出现看多交叉时，策略买入 `OrderVolume` 合约。如果已有空头仓位，订单数量会自动增加，既平掉空头又建立新的多头头寸。
5. 出现看空交叉时，策略卖出 `OrderVolume` 合约。如果已有多头仓位，订单数量会相应增加以在建立空头之前平掉多头。
6. 调用 `StartProtection()` 以便根据需要附加 StockSharp 的保护模块（例如止损或保本模块）。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `FirstPeriod` | 快速移动平均线的周期。 | `3` |
| `SecondPeriod` | 慢速移动平均线的周期。 | `13` |
| `FirstMethod` | 快速移动平均线采用的平滑方法（`Simple`、`Exponential`、`Smoothed`、`LinearWeighted`）。 | `Simple` |
| `SecondMethod` | 慢速移动平均线的平滑方法。 | `LinearWeighted` |
| `FirstPriceMode` | 快速移动平均线使用的价格类型（`Close`、`Open`、`High`、`Low`、`Median`、`Typical`、`Weighted`）。 | `Close` |
| `SecondPriceMode` | 慢速移动平均线使用的价格类型。 | `Median` |
| `FirstShift` | 快速移动平均线的水平位移（柱数）。 | `0` |
| `SecondShift` | 慢速移动平均线的水平位移。 | `0` |
| `OrderVolume` | 新仓位使用的基础下单手数。 | `0.1` |
| `CandleType` | 策略处理的 K 线类型/周期。 | 5 分钟 K 线 |

## 与 MQL 版本的差异
- MetaTrader 中的订单遍历 (`OrdersTotal`、`OrderSelect`、`OrderClose`) 被替换为直接读取 StockSharp 的 `Strategy.Position` 属性，并按照需要反向开仓的手数发送市价单。
- 原版利用“新柱”标志避免重复下单；在移植版中，`ProcessCandle` 仅在每根已完成的 K 线上调用一次，从事件驱动角度自然实现“一柱一次”的行为。
- 通过保留最近 `shift + 2` 个指标值的精简缓存来模拟 MA 位移，无需使用被禁止的指标回溯方法（例如 `GetValue`）。
- 策略本身不绑定经纪商风控，使用 `StartProtection()` 可以接入 StockSharp 的任意保护模块，而不是 MetaTrader 固定的止损/止盈参数。

## 使用说明
- 选择与原策略一致的周期（例如 M5、H1）。也可以在参数中直接指定其他时间框架。
- 将 `FirstShift` 或 `SecondShift` 设置为正值会让交叉信号延后相应数量的已完成柱，与 MetaTrader 中的水平偏移完全一致。
- `Weighted` 价格模式复现了 MetaTrader 中的 `(High + Low + 2 * Close) / 4` 公式；`Median` 与 `Typical` 分别对应 `(High + Low) / 2` 和 `(High + Low + Close) / 3`。
- 所有订单均为市价单，请确保账户设置允许相应的手数及可能的滑点。
