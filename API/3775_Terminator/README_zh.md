# Terminator 策略

## 概述

Terminator 策略在 StockSharp 高级 API 中复现了 MetaTrader 4 "Terminator v2.0" 智能交易程序的网格马丁结构。策略根据 MACD 斜率判定方向，在行情向持仓不利移动指定点数时分批加仓。仓位通过可选的止损、止盈、移动止损以及安全利润保护规则进行管理，以便在浮动盈利达到目标时锁定收益。

## 交易逻辑

1. **信号生成**：在每根已完成的 K 线结束时读取 MACD 主线。如果当前值高于上一根柱体，则判定为多头倾向；如果低于上一根柱体，则判定为空头倾向。`ReverseSignals` 参数可以反转这种判断。
2. **初始入场**：当没有持仓并且时间过滤器（`StartYear`、`StartMonth`、`EndYear`、`EndMonth`）允许交易时，策略按检测到的方向下单，除非启用了 `ManualTrading` 手动模式。
3. **马丁加仓**：若已存在网格仓位，策略等待价格向不利方向移动 `EntryDistancePips` 点，然后再次入场。每次加仓的手数为上一次的两倍（`MaxTrades` 大于 12 时使用 1.5 倍），直到达到 `MaxTrades` 限制。启用 `UseMoneyManagement` 后，初始手数可根据账户余额与 `RiskPercent` 计算。
4. **风险控制**：
   - **止盈**：`TakeProfitPips` 定义整个篮子的止盈距离。
   - **初始止损**：`InitialStopPips` 可为整篮持仓设置初始止损，设置为 0 则禁用。
   - **移动止损**：当利润至少达到移动距离加一次加仓间距时，`TrailingStopPips` 会推动止损沿趋势方向移动。
   - **账户保护**：启用 `UseAccountProtection` 且持仓数量达到 `MaxTrades - OrdersToProtect` 时，会把浮动盈利与 `SecureProfit`（若 `ProtectUsingBalance` 为真则使用当前账户权益）比较。若超过阈值，则平掉最后一次加仓并禁止继续开仓，以锁定收益。
5. **篮子重置**：净持仓归零后会清除所有内部计数，等待下一轮交易机会。

## 参数

- `TakeProfitPips`：整篮止盈点数。
- `InitialStopPips`：初始止损点数（0 表示关闭）。
- `TrailingStopPips`：移动止损点数（0 表示关闭）。
- `MaxTrades`：允许同时存在的最大马丁加仓次数。
- `EntryDistancePips`：每次加仓所需的不利移动点数。
- `SecureProfit`：安全利润阈值（货币单位）。
- `UseAccountProtection`：启用安全利润保护模块。
- `ProtectUsingBalance`：使用当前账户权益作为保护阈值，替代 `SecureProfit`。
- `OrdersToProtect`：接近尾声的加仓数量，用于触发保护逻辑。
- `ReverseSignals`：反向解释 MACD 斜率。
- `ManualTrading`：关闭自动入场，仅保留仓位管理。
- `LotSize`：未启用资金管理时的固定手数。
- `UseMoneyManagement`：启用资金管理，根据账户余额和 `RiskPercent` 计算初始手数。
- `RiskPercent`：资金管理使用的风险百分比（基于 100%）。
- `IsStandardAccount`：选择标准账户还是迷你账户手数换算。
- `EurUsdPipValue`、`GbpUsdPipValue`、`UsdChfPipValue`、`UsdJpyPipValue`、`DefaultPipValue`：用于换算浮动盈亏的点值。
- `StartYear`、`StartMonth`、`EndYear`、`EndMonth`：限制允许开仓的时间范围。
- `CandleType`：用于计算信号的 K 线类型。
- `MacdFastLength`、`MacdSlowLength`、`MacdSignalLength`：MACD 指标参数。

## 使用说明

- 策略只处理 `CandleType` 指定周期的已完成 K 线。
- 为了贴近原版 EA，请根据交易品种调整点值参数。
- `ManualTrading` 开启时仍会执行移动止损和账户保护，方便手动管理头寸。
- 原 EA 的其他入场模式依赖自定义指标，因此本转换仅实现 MACD 方案。

## 转换细节

- 资金管理、网格间距、马丁倍数和安全利润逻辑严格参考 MQ4 源码。
- MT4 中的 `AccountProtection` 与 `AllSymbolsProtect` 被映射为 `UseAccountProtection` 与 `ProtectUsingBalance`。
- `ReverseCondition` 与 `Manual` 对应 `ReverseSignals` 与 `ManualTrading`。
- 止损与移动止损针对整个仓位而非单个订单，与原始 EA 行为一致。

## 运行步骤

1. 在 Visual Studio 中打开解决方案。
2. 将策略添加到 `StrategyRunner` 或 `StrategyConnector`。
3. 在界面或代码中配置参数。
4. 启动策略后，会自动订阅所需的 K 线并执行信号判断。
