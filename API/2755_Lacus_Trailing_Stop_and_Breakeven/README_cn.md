# Lacus Trailing Stop & Breakeven 风险管理器

## 概述
本策略是 MetaTrader 专家顾问 **LacusTstopandBE.mq5** 的移植版本。策略本身 **不会** 主动开仓，而是负责管理已经存在的净持仓，具体包括：

- 设置止损与止盈（或在隐身模式下模拟这些价格），
- 在盈利达到指定值后将止损移动到保本位置，
- 当价格继续向有利方向运行时启动移动止损，
- 在达到单笔利润目标时平仓，
- 当达到全局利润目标（绝对金额或账户百分比）时平仓。

策略基于 StockSharp 的高级 API（`SubscribeCandles`），仅针对单一标的运行。只有在未启用隐身模式时才会发送保护性订单（`SellStop`、`BuyLimit` 等）。

## 交易逻辑
1. **初始保护**：持仓从空仓变为多/空后，根据输入的点数计算止损、止盈价格，并发送相应订单；若启用隐身模式，则仅保存目标价格。
2. **保本处理**：当浮动盈利达到 `BreakevenGainPips` 时，将止损移动到入场价 ± `BreakevenLockPips`（仅在锁定距离小于触发距离时生效）。
3. **移动止损**：当价格已走出 `TrailingStartPips` 时，止损以 `TrailingStopPips` 的距离跟随价格，且只朝有利方向移动。
4. **隐身执行**：隐身模式下，策略在每根已完成的K线后检查隐藏的止损/止盈价位，并在触发后通过市价平仓。
5. **利润目标**：
   - `PositionProfitTarget`：当单笔 mark-to-market 盈利超过设定金额时平仓；
   - `ProfitAmount`：当策略总盈亏达到设定金额时平仓；
   - `PercentOfBalance`：当当前组合价值较启动时增长到指定百分比时平仓。

## 参数
| 参数 | 说明 | 默认值 | 备注 |
|------|------|--------|------|
| `StopLossPips` | 止损距离（点）。 | 40 | 与 `Security.PriceStep` 相乘换算成价格。 |
| `TakeProfitPips` | 止盈距离（点）。 | 200 | 与 `Security.PriceStep` 相乘。 |
| `TrailingStartPips` | 启动移动止损所需盈利（点）。 | 30 | 设为 0 可关闭移动止损。 |
| `TrailingStopPips` | 移动止损的跟随距离。 | 20 | 需与 `TrailingStartPips` 配合使用。 |
| `BreakevenGainPips` | 触发保本所需盈利。 | 25 | 必须大于 `BreakevenLockPips`。 |
| `BreakevenLockPips` | 保本后锁定的点数。 | 10 | 设为 0 将止损移至入场价。 |
| `PositionProfitTarget` | 单笔利润目标（货币）。 | 4 | 基于 mark-to-market 盈亏。 |
| `ProfitAmount` | 全局利润目标（货币）。 | 12 | 基于 `Strategy.PnL`。 |
| `PercentOfBalance` | 账户价值的利润百分比目标。 | 1 | 使用 `Portfolio.CurrentValue`。 |
| `UseStealthStops` | 是否在隐身模式下模拟止损/止盈。 | `false` | 仅在 K 线收盘时检查触发。 |
| `CandleType` | 使用的K线类型。 | 1分钟K线 | 可根据需要调整频率。 |

## 注意事项
- 策略假设账户为 **净持仓模式**；若经纪商支持逐仓对冲，需要额外改造。
- 隐身模式只在已完成的K线上检测触发，需选择足够小的时间框架确保响应速度。
- 全局利润目标依赖 `Portfolio.CurrentValue`，请确认连接的适配器能够提供该字段。
- 原 MQL 脚本中的佣金与掉期调整不可用，策略仅使用 mark-to-market 盈亏。
- 启动策略前请确保策略的下单量与现有持仓数量一致，以保持行为一致。

## 转换说明
- MQL 中的 `SetSLTP`、`Movebreakeven`、`TrailingStop`、`CloseOnProfit`、`CloseAll`、`CloseonStealthSLTP` 分别实现为独立的 C# 方法。
- 点值转换通过 `Security.PriceStep` 完成，对应 MetaTrader 中的 `SymbolInfo.Point()`。
- `AccountInfo` 的盈利检查替换为 `Strategy.PnL` 与组合价值变化的组合。
- StockSharp 不需要魔术号，因此该逻辑被移除。
- 所有代码注释均按要求改写为英文。
