# Couple Hedge 策略

## 概览

Couple Hedge 是一套多货币对对冲策略，源自原始的 *MT5 CoupleHedgeEA v3.0*。
策略将两个品种组成一组（分别称为 “plus” 与 “minus” 侧），同步交易以捕捉
相对价差，同时保持组合整体对冲。策略以账户货币计算篮子收益，当篮子进入
亏损时按预设规则加仓，在达到收益或亏损目标后平掉全部仓位。

## 主要特性

- **篮子交易。** 每个组同时建立多头和空头仓位，构成对冲篮子，并可按几何或
  指数规则继续加仓。
- **利润管理。** 利润来自 StockSharp 提供的单个仓位 PnL。盈利/亏损延迟以秒为
  单位，复刻 MQL 版本中的 tick 延迟逻辑。
- **交易时段控制。** 在周一开盘后与周五收盘前的指定时间段暂停交易。
- **点差过滤。** 当平均点差超过阈值时跳过加仓。
- **自动仓位。** 启用自动手数后，首个篮子根据投资组合市值的百分比计算，后续
  仓位按所选进阶方式放大。

## 参数说明

| 参数 | 说明 |
|------|------|
| `OperationMode` | 总体运行模式。`CloseImmediatelyAllOrders` 立即平仓，`CloseInProfitAndStop` 在盈利一次后停止。 |
| `SideSelection` | 设置同时交易两侧还是只交易一侧。 |
| `StepMode` | 加仓规则。`OpenWithAutoStep` 使用蜡烛区间的 EMA 估算触发距离。 |
| `StepOpenNext` | 触发下一次加仓所需的篮子亏损（账户货币）。 |
| `StepProgression`、`StepProgressionFactor` | 控制触发距离在多次加仓时的增长方式。 |
| `MinutesBetweenOrders` | 同一组两次下单之间的最小等待时间。 |
| `CloseProfitMode`、`TargetCloseProfit` | 盈利退出逻辑。`SideBySide` 独立监控每一侧。 |
| `CloseLossMode`、`TargetCloseLoss` | 亏损退出逻辑。 |
| `DelayCloseProfit` / `DelayCloseLoss` | 盈利/亏损触发后的延迟（秒）。 |
| `AutoLot`、`RiskFactor`、`ManualLotSize` | 仓位管理。自动模式会用权益百分比除以最新中间价。 |
| `LotProgression`、`ProgressionFactor` | 加仓手数的增长方式。 |
| `UseFairLotSize` | 根据品种的 `TickValue` 平衡多空手数。 |
| `MaximumLotSize` | 单侧最大手数，0 表示不限。 |
| `MaximumGroups` | 同时交易的组数量限制。 |
| `MaximumOrders` | 总持仓数量限制。 |
| `MaxSpread` | 平均点差上限（以绝对价格表示）。 |
| `ControlSession`、`WaitAfterOpen`、`StopBeforeClose` | 复现原始 EA 的时段限制设置。 |

`GroupNEnabled`、`GroupNPlus`、`GroupNMinus`、`GroupNCandleType` 等分组参数
允许配置三个默认组，也可以在界面中手动选择其他品种。

## 执行流程

1. 为所有启用的品种订阅蜡烛和 Level1 数据。
2. 更新蜡烛区间平均值与中间价，为自动加仓和自动手数提供输入。
3. 根据当前持仓计算篮子 PnL，并在突破盈利/亏损阈值时排队等待延迟关闭。
4. 检查 `MaximumGroups`、`MaximumOrders`、`MinutesBetweenOrders` 等限制。
5. 当篮子亏损超过触发值时，在选定的方向开出新的对冲仓位，手数按进阶规则
   （及可选的 tick value 平衡）调整。
6. 当盈利或亏损信号在延迟后确认，平掉该组所有仓位。

## 使用建议

- 确保所选投资组合可以交易全部品种，并提供实时 Level1 数据以监控点差。
- 日志会记录每次开仓的篮子编号，方便与 MT5 版本对比。
- `MaxSpread` 使用绝对价格，外汇五位报价时 `0.0004` 约等于 4 个点。
- 启用 `UseFairLotSize` 时需保证品种的 `TickValue` 已正确设置。
- 会话过滤使用本地时间，如需按照交易所时间请自行调整参数。

## 与 MQL 版本的区别

- 加仓触发值以账户货币表示，而非“每手点数”，与 StockSharp 的账户模型一致。
- Tick 延迟转换为秒。若需接近原始行为，可使用 1–5 秒的小延迟。
- 图表界面相关的参数（`SetChartInterface`、`SaveInformation`）仅保留为信息项。

## 快速上手

1. 将策略文件加入 StockSharp 解决方案并重新编译。
2. 把策略连接到交易端，选择投资组合，确认所有品种有 Level1 行情。
3. 在界面中设置各组的品种及风险参数。
4. 启动策略，观察日志中的加仓/平仓记录，根据风险偏好微调进阶设置。

