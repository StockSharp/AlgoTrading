# Couple Hedge 策略
[English](README.md) | [Русский](README_ru.md)

Couple Hedge 策略从 **MT4 CoupleHedgeEA v2.5** 移植而来，是一个多品种对冲网格系统。用户按强弱顺序提供货币列表，系统会生成所有可能的货币对并在每个组合上构建对冲篮子。根据相对强弱，篮子会分别开多（Plus）或开空（Minus），并通过“步长”和“进阶”设置在亏损时逐步加仓。

StockSharp 版本专注于执行逻辑：图形界面和大量面板信息被移除，但保留了所有关键参数——运行模式、补仓逻辑、盈亏管理以及手数进阶方式。

## 原始思路
- 同时交易多条由三至五种货币构成的货币对，提高分散度。
- 维持多头篮子和空头篮子，根据货币强弱排名动态再平衡。
- 在浮亏阶段按设定的步长和手数进阶继续加仓，直至整体篮子转盈。
- 当达到按手数计算的盈利目标时平仓，可设置延迟以过滤噪音。

## StockSharp 实现
- 根据 `CurrencyTrade` 字符串生成所有货币对（如 `EUR/GBP/USD` 会创建 EURGBP、EURUSD、GBPUSD）。若找不到对应证券则跳过，必要时回退到主图品种。
- 使用 `TrendPeriod` 周期的均线和 `AtrPeriod` 周期的 ATR 衡量强弱。只有当 ATR 标准化偏离超过 `SignalThreshold` 时才会开新篮子。
- 支持原策略的方向过滤（`TradeOnlyPlus`、`TradeOnlyMinus`、`TradePlusAndMinus`）。Plus 篮子买入该品种，Minus 篮子卖出。
- 当浮亏（按手数折算）达到当前步长阈值时加仓。手动模式直接使用 `StepOpenNextOrders`，自动模式将该值与 ATR 相乘。步长与手数的 `Statical`、`Geometrical`、`Exponential` 进阶均可选。
- 盈利与亏损退出均实现了原有票据与篮子两种逻辑，并通过 `DelayCloseProfit`、`DelayCloseLoss` 模拟等待机制。
- 亏损处理可以选择全部平仓或仅减掉首笔仓位。
- 自动手数根据 `RiskFactor` 估算为权益的一定比例；关闭后使用固定的 `ManualLotSize`。

## 交易规则
- **入场**
  - 计算偏离度 = (收盘价 − SMA) / ATR。
  - 若偏离度 ≥ `SignalThreshold` 且允许做多，则开多篮子。
  - 若偏离度 ≤ −`SignalThreshold` 且允许做空，则开空篮子。
- **加仓**
  - 当 `OpenOrdersInLoss` ≠ `NotOpenInLoss` 时启用。
  - 浮亏（按每手）达到当前步长阈值后触发。
  - 加仓手数依赖所选进阶模式，遵循 `MaximumOrders` 限制（0 表示无限制）。
- **盈利退出**
  - Ticket 模式：单个品种盈利达到 `TargetCloseProfit` 持续 `DelayCloseProfit` 根 K 线后平仓。
  - Basket 模式：所有监控品种的合计盈利达到目标并满足延迟后统一平仓。
  - Hybrid 模式需要同时满足 Ticket 与 Basket 条件。
  - 若 `TypeOperation` = `CloseInProfitAndStop`，盈利平仓后策略停止运行。
- **亏损退出**
  - 浮亏（按每手）≥ `TargetCloseLoss` 且持续 `DelayCloseLoss` 根 K 线时触发。
  - `WholeTicket` 全部平仓；`OnlyFirstOrder` 只减掉首单；`NotCloseInLoss` 忽略信号。

## 参数（默认值）
- `TypeOperation` = `NormalOperation`
- `OpenOrdersInLoss` = `OpenWithAutoStep`
- `StepOpenNextOrders` = 50（美元/手）
- `StepOrdersProgress` = `GeometricalStep`
- `LotOrdersProgress` = `StaticalLot`
- `TypeCloseInProfit` = `BasketOrders`
- `TargetCloseProfit` = 50（美元/手）
- `DelayCloseProfit` = 3 根 K 线
- `TypeCloseInLoss` = `NotCloseInLoss`
- `TargetCloseLoss` = 1000（美元/手）
- `DelayCloseLoss` = 3 根 K 线
- `MaximumOrders` = 0（无限制）
- `SideToOpenOrders` = `TradePlusAndMinus`
- `CurrencyTrade` = `EUR/GBP/USD`
- `AutoLotSize` = false
- `RiskFactor` = 0.1（自动手数时使用 10% 权益）
- `ManualLotSize` = 0.01
- `TrendPeriod` = 34
- `AtrPeriod` = 14
- `SignalThreshold` = 0.3
- `CandleType` = 15 分钟周期

## 限制说明
- 未移植原版的分组、跳过列表、界面颜色、交易时段控制和日志保存功能。
- MT4 的毫秒定时器被蜡烛订阅所替代，不再提供独立计时。
- 自动手数采用简化的权益百分比模型，因为 StockSharp 无法获取 MT4 账户杠杆信息。
- 需要交易连接器提供逐仓 PnL，否则篮子平仓逻辑可能无法触发。
- 按要求未提供 Python 版本。
