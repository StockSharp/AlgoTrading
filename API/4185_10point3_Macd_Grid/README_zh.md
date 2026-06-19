# TenPointThree MACD 网格策略

## 概览

该策略是 MetaTrader 专家顾问 **10p3v003 (10point3.mq4)** 的 C# 版本。它把 MACD 金叉/死叉信号与马丁格尔式网格加仓结合，并通过 StockSharp 的高级 API 重现原脚本的全部流程：

- **MACD 触发**：在 `SignalShift` 指定的已完成 K 线上，当 MACD 主线突破信号线且满足信号阈值 (`TradingRangePips`) 时生成方向。做多要求上一根信号值低于 `-TradingRangePips`、当前 MACD 小于 0，做空则相反。通过 `ReverseSignal` 可反转方向。
- **网格加仓**：首单之后，只有当价格相对上一单逆行至少 `GridStepPips` 点时才允许继续加仓。每一笔新单的手数乘以 `LotMultiplier`（若 `MaxTrades > 12` 自动改用 1.5），完整复刻了原 EA 的马丁格尔手数放大方式。
- **收益保护**：当持仓数达到 `OrdersToProtect` 且浮动收益超过阈值时，立即平掉最新一单并暂停继续加仓。阈值依据是否启用资金管理分别取账户风险百分比或合约规模推算。
- **逐单退出**：每笔网格单都独立跟踪止盈、虚拟止损与移动止损。止损距离沿用原公式：`InitialStopPips + (MaxTrades - existingOrders) * GridStepPips`。移动止损需先获得 `TrailingStopPips + GridStepPips` 的盈利空间，之后若回撤 `TrailingStopPips` 即平仓。
- **时间过滤**：启用 `UseTimeFilter` 时，如果蜡烛时间严格处于 `StopHour` 与 `StartHour` 之间，则不会开启新的网格，从而再现原策略的“危险时段”过滤器。

所有货币换算都依赖品种的 `PriceStep` / `StepPrice`。若交易所未提供合约面值，则使用 100000 的默认值，与外汇标准合约保持一致。

## 参数

| 参数 | 说明 |
| ---- | ---- |
| `CandleType` | 用于驱动 MACD 的 K 线类型（默认：30 分钟）。 |
| `Volume` | 首单的基础手数。 |
| `TakeProfitPips` | 单笔止盈点数（0 表示关闭）。 |
| `InitialStopPips` | 初始止损点数，会按照剩余网格空间自动拉大。 |
| `TrailingStopPips` | 移动止损点数（0 表示关闭）。 |
| `MaxTrades` | 允许同时存在的最大网格单数量。 |
| `LotMultiplier` | 每次加仓时的手数放大倍数（当 `MaxTrades > 12` 时固定为 1.5）。 |
| `GridStepPips` | 触发下一笔加仓所需的最小逆向点数。 |
| `OrdersToProtect` | 启动收益保护所需的最少持仓数量。 |
| `UseMoneyManagement` | 启用基于权益的动态手数计算。 |
| `AccountType` | 选择风险公式：`0` 标准账户（权益/10000）、`1` 普通账户（权益/100000）、`2` Nano 账户（权益/1000）。 |
| `RiskPercent` | 启用资金管理时使用的权益百分比。 |
| `ReverseSignal` | 反转 MACD 信号方向。 |
| `FastEmaLength`、`SlowEmaLength`、`SignalLength` | MACD 参数（默认 12/26/9）。 |
| `SignalShift` | 用于判定交叉的历史 K 线偏移量（默认 1）。 |
| `TradingRangePips` | MACD 信号需要突破的带宽阈值。 |
| `UseTimeFilter` | 启用危险时间过滤。 |
| `StopHour`、`StartHour` | 危险区间的下限/上限（严格不含端点）。 |

## 资金管理说明

关闭 `UseMoneyManagement` 时，始终使用固定 `Volume`。打开后按原 EA 公式计算手数：

- 类型 **0**：`Ceil(risk% * equity / 10000) / 10`
- 类型 **1**：`risk% * equity / 100000`
- 类型 **2**：`risk% * equity / 1000`

计算结果会按照 `Security.VolumeStep` 取整，并受到 `MinVolume` / `MaxVolume` 限制。

## 执行流程

1. 订阅配置的 K 线并通过 `BindEx` 驱动 MACD 指标。
2. 每根完成的 K 线更新所有网格单的止盈/止损/移动止损。
3. 当 MACD 交叉满足条件时，检查时间过滤、方向一致性以及价格是否逆行到 `GridStepPips`，随后按马丁格尔手数下单。
4. 持续监控浮动盈亏；一旦超过保护阈值则平掉最新网格单，并等待下一根 K 线后再评估。

## 转换说明

- 所有代码注释均改为英文，符合仓库规范。
- 完整使用 StockSharp 高阶接口（K 线订阅 + `BindEx`）。
- 浮动盈亏依赖 `PriceStep` / `StepPrice`，使用非外汇品种时请确认元数据完整。
- 为模拟 MQL4 的多订单管理，策略内部维护每笔网格单的独立状态，避免 StockSharp 汇总持仓带来的信息缺失。
