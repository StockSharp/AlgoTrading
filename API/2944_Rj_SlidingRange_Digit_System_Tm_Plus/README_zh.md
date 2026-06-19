# Exp Rj SlidingRangeRj Digit System Tm Plus 策略

## 概述

该策略是 MetaTrader 专家顾问 `Exp_Rj_SlidingRangeRj_Digit_System_Tm_Plus` 的 StockSharp 版本，实现了原始 EA 的交易逻辑与参数。策略基于自定义的 **Rj_SlidingRangeRj_Digit** 通道指标，实时订阅所选周期的收盘K线，识别价格突破通道后的信号，并按照原方案执行延迟进场、时间退出以及点数止损/止盈管理。

## 指标原理

Rj_SlidingRangeRj_Digit 指标通过多次滑动窗口运算构造自适应通道：

1. **上轨**：对最近 `UpCalcPeriodRange` 个滑动窗口（每个窗口长度同样为 `UpCalcPeriodRange`，并可通过 `UpCalcPeriodShift` 偏移）分别取最高价，再对这些最高价求平均，并根据 `UpDigit` 指定的小数位进行四舍五入。
2. **下轨**：采用相同方法处理最低价，参数为 `DnCalcPeriodRange`、`DnCalcPeriodShift` 和 `DnDigit`。
3. 当K线收盘价高于上轨时，颜色编号为 `2` 或 `3`；收盘价低于下轨时，颜色编号为 `0` 或 `1`；处于通道内部时为 `4`。

策略每根收盘K线都重新计算通道，并保存最近几根K线的颜色标签，以复刻 MQL 版本中 `CopyBuffer` + `SignalBar` 的读取方式。

## 交易逻辑

* **信号延迟：** 使用 `SignalBar` 指定的历史K线（默认上一根）进行判定，仅当该K线出现突破颜色且再前一根K线没有相同颜色时才触发交易，确保与原EA一样在下一根K线开盘处理信号。
* **多头进场：** 当 `EnableBuyEntries` 为真且检测到多头突破（颜色 2 或 3）时，在当前无多头仓位的情况下市价买入。若存在空头仓位，会自动加量平仓并反向开仓。
* **空头进场：** 当 `EnableSellEntries` 为真且颜色为 0 或 1 时市价卖出，逻辑对称。
* **离场规则：**
  * `EnableBuyExits` 控制多头是否在出现空头颜色（0 或 1）时离场。
  * `EnableSellExits` 控制空头是否在出现多头颜色（2 或 3）时离场。
  * `UseTimeExit` 为真时，持仓时间超过 `ExitMinutes`（分钟）自动平仓。
  * `StopLossPoints` 与 `TakeProfitPoints` 以“点”为单位设置止损/止盈，系统会根据 `Security.PriceStep` 换算成价格差。

所有指令均使用 `BuyMarket` / `SellMarket`，既可关闭已有仓位也能直接反手建仓。

## 参数

| 参数 | 含义 | 默认值 |
|------|------|--------|
| `CandleType` | 用于生成信号的K线类型/周期 | 8 小时K线 |
| `EnableBuyEntries` / `EnableSellEntries` | 是否允许多头/空头进场 | `true` |
| `EnableBuyExits` / `EnableSellExits` | 是否允许指标离场信号 | `true` |
| `UseTimeExit` | 是否启用时间止盈/止损 | `true` |
| `ExitMinutes` | 持仓最长时间（分钟） | `1920` |
| `UpCalcPeriodRange`, `UpCalcPeriodShift`, `UpDigit` | 上轨参数 | `5`, `0`, `2` |
| `DnCalcPeriodRange`, `DnCalcPeriodShift`, `DnDigit` | 下轨参数 | `5`, `0`, `2` |
| `SignalBar` | 回看多少根K线确认信号 | `1` |
| `StopLossPoints`, `TakeProfitPoints` | 点数止损/止盈（乘以 `PriceStep` 换算价格） | `1000`, `2000` |

仓位大小通过策略的 `Volume` 属性配置。将止损或止盈设为 `0` 即可禁用对应保护。

## 注意事项

* 通道计算需要足够的历史K线（大约 `max(shift + 2 × range)` 根），策略会在数据不足时自动跳过信号。
* 价格取整按照小数位数实现，效果与 MQL 指标的四舍五入一致。
* 根据项目要求，本目录仅提供 C# 实现，未提供 Python 版本。
