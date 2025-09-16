# Ten Points 3 策略

## 概述
该策略是 MetaTrader 4 经典专家顾问“10 points 3”的 StockSharp 版本。它实现了一种顺势加仓的马丁策略，通过观察 MACD 主线的斜率来决定当前交易周期是做多还是做空。一旦方向确定，当价格朝不利方向移动到设定距离时，策略就会逐步增加新的仓位，并持续持有直到达到止盈或触发保护条件。

## 交易逻辑
- 根据 MACD 主线最近两根柱子的变化确定交易方向：值上升时准备买入，下跌时准备卖出，可以通过参数反向解读。
- 只要方向成立且交易日历（起止年份与月份）允许，就立即开出第一笔仓位。
- 持仓期间，每当价格逆势运行 `EntryDistancePips` 点，就按马丁规则加仓。若 `MaxTrades` 大于 12，手数乘以 1.5，否则乘以 2。
- 每笔新单都会带有 `InitialStopPips` 和 `TakeProfitPips` 距离的初始止损和止盈，策略对整组仓位维护聚合的保护水平。
- 当浮盈达到 `TrailingStopPips + EntryDistancePips` 点时启动移动止损，之后按 `TrailingStopPips` 的距离追踪价格。
- 当持仓数量达到 `MaxTrades - OrdersToProtect` 时启用账户保护模块。策略会按品种对应的点值计算浮动收益，若收益超过 `SecureProfit`，则平掉最后一笔仓位并暂停继续加仓，直到整组仓位全部平仓。

## 资金管理
- 基础手数可以固定为 `LotSize`，也可以启用原策略的资金管理公式。当 `UseMoneyManagement` 为真时，策略会读取当前投资组合价值并结合 `RiskPercent` 计算基础手数。关闭 `IsStandardAccount` 可以模拟迷你账户。
- 计算得到的基础手数在应用马丁倍数之前会被限制在 100 手以内。

## 订单管理
- 所有进场都使用市价单（`BuyMarket`/`SellMarket`）。
- 保护性离场在达到计算出的止损、移动止损或止盈价格时立即用市价单执行。
- 账户保护仅平掉最近一笔订单（`_lastEntryVolume`），与原版 EA 的行为一致。

## 参数说明
| 参数 | 说明 |
|------|------|
| `TakeProfitPips` | 每笔订单的止盈距离。|
| `LotSize` | 未启用资金管理时使用的固定手数。|
| `InitialStopPips` | 每笔订单的初始止损距离。|
| `TrailingStopPips` | 触发后追踪的移动止损距离。|
| `MaxTrades` | 同一篮子最多允许的加仓次数。|
| `EntryDistancePips` | 每次加仓需要的最小逆势点数。|
| `SecureProfit` | 触发账户保护所需的浮动收益（货币单位）。|
| `UseAccountProtection` | 是否启用账户保护逻辑。|
| `OrdersToProtect` | 在账户保护阶段需要保留的末端马丁阶数。|
| `ReverseSignals` | 反转 MACD 斜率的解读方向。|
| `UseMoneyManagement` | 是否启用资金管理。|
| `RiskPercent` | 资金管理公式使用的风险百分比。|
| `IsStandardAccount` | 是否按标准手数（非迷你）计算。|
| `EurUsdPipValue`、`GbpUsdPipValue`、`UsdChfPipValue`、`UsdJpyPipValue`、`DefaultPipValue` | 计算浮动收益时使用的点值。|
| `StartYear`、`StartMonth`、`EndYear`、`EndMonth` | 限定可以开启新篮子的时间范围。|
| `CandleType` | 用于生成信号和管理仓位的 K 线周期。|
| `MacdFastLength`、`MacdSlowLength`、`MacdSignalLength` | MACD 指标的三个周期参数。|

## 说明
- 策略使用 StockSharp 的高层 API（`SubscribeCandles` + `BindEx`）重现原 EA 的马丁倍数与保护逻辑。
- 浮动收益的计算基于聚合仓位，因此与 MetaTrader 修改单笔挂单的方式相比，最终结果可能存在细微差异。
- 在真实账户运行前，请根据交易品种确认点值、最小变动价位与手数单位是否与参数匹配。
