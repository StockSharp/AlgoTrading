# 趋势角度线策略

## 概述

该策略是 MetaTrader 智能交易系统 *Trend Line By Angle* 的 StockSharp 版本。原始脚本依赖人工按键下单并配备了复杂的资金管理。本移植将人为流程转换为自动化的 MACD 趋势策略，同时保留所有保护组件：

- 在 `SignalCandleType` 周期上计算 12/26/9 MACD，金叉触发做多，死叉触发做空。
- 按照 `TradeVolume * MaxEntries` 的目标仓位分批进场，模拟原始 EA 多次点击按钮的行为。
- 在交易周期上运行 20/2 布林带，价格触及上轨立即平掉多单，触及下轨平掉空单。
- 传统风险控制——止损、止盈、移动止损、保本位——全部以 `PriceStep` 转换出的点差计算。
- 账户级别保护包含绝对盈利目标、百分比盈利目标以及资金跟踪止损。

## 执行流程

1. **指标准备**：`MovingAverageConvergenceDivergenceSignal` 绑定信号周期，`BollingerBands` 绑定执行周期。
2. **入场信号**：每根执行周期 K 线收盘后评估最近一次 MACD 交叉，金叉调用 `BuyMarket`，死叉调用 `SellMarket`。若存在相反持仓，会先平仓再反向建仓。
3. **分批进场**：持续加仓直至净仓位达到 `TradeVolume * MaxEntries`。
4. **风险管理**：每根 K 线重新计算保本位、移动止损、止损与止盈；价格触碰布林带也会立即离场。
5. **账户保护**：在生成新信号前先检查资金目标；资金跟踪模块记录最大总盈亏并在回撤超过 `MoneyTrailStop` 时平仓。

## 资金管理细节

- **总盈亏** 等于已实现盈亏 (`PnL`) 加上根据收盘价、最小变动价位和点值计算的浮动盈亏。
- **保本位** 在盈利超过 `BreakEvenTriggerPips` 后，将止损移动到 `Entry ± BreakEvenOffsetPips`。
- **移动止损** 在盈利超过 `TrailingStopPips` 时向价格逼近，多单止损只会上移，空单止损只会下移。
- **资金跟踪止损** 在盈利达到 `MoneyTrailTrigger` 后启动，记录峰值，若回撤超过 `MoneyTrailStop` 则全部平仓。

## 参数

| 参数 | 说明 |
| --- | --- |
| `TradeVolume` | 单次下单量。 |
| `MaxEntries` | 累积下单的最大份数。 |
| `StopLossPips` | 止损距离（点）。 |
| `TakeProfitPips` | 止盈距离（点）。 |
| `TrailingStopPips` | 移动止损距离（点）。 |
| `UseBreakEven` | 是否启用保本位。 |
| `BreakEvenTriggerPips` | 触发保本位所需的盈利。 |
| `BreakEvenOffsetPips` | 移动到保本位时额外增加的点数。 |
| `UseBollingerExit` | 是否根据布林带触发离场。 |
| `BollingerPeriod` / `BollingerDeviation` | 布林带参数。 |
| `UseProfitMoneyTarget` / `ProfitMoneyTarget` | 账户货币盈利目标开关及其值。 |
| `UseProfitPercentTarget` / `ProfitPercentTarget` | 账户百分比盈利目标开关及其值。 |
| `EnableMoneyTrail` | 是否启用资金跟踪止损。 |
| `MoneyTrailTrigger` | 资金跟踪止损开始工作的盈利阈值。 |
| `MoneyTrailStop` | 与峰值相比允许的回撤。 |
| `MacdFastPeriod`、`MacdSlowPeriod`、`MacdSignalPeriod` | MACD 设置。 |
| `CandleType` | 执行周期。 |
| `SignalCandleType` | 计算 MACD 的周期。 |

## 使用说明

- 请确保交易品种的 `PriceStep` 与 `StepPrice` 已正确设置，否则点值换算会出错。
- 如果投资组合没有提供 `Portfolio.CurrentValue` 或 `Portfolio.BeginValue`，百分比盈利目标会自动忽略。
- C# 源码内的所有注释均为英文，便于国际化协作与维护。
