# RRS 随机策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 概述

**RRS Randomness Strategy** 是 MetaTrader 4 上 “RRS Randomness in Nature EA” 的 StockSharp 移植版。  
策略会伪随机地打开多头或空头市价单，在 K 线完成后检查止损与止盈，可选地启动跟踪止损，并在浮动亏损达到设定阈值时强制平仓。

由于 StockSharp 对同一品种采用净持仓模式，不能同时持有多头和空头仓位。因此 `DoubleSide` 模式从做多开始并在每次入场后交替方向，而不是像 MetaTrader 那样同时维持两笔对冲交易。

## 交易逻辑

1. 每根 K 线完成后，策略使用收盘价检查保护条件；如有 Level1 bid/ask，则用其计算点差和清算价格。
2. 如果存在持仓，则依次检查止损、止盈、跟踪止损和浮动亏损上限；每根 K 线最多提交一笔平仓单。
3. 在空仓状态下，先检查点差与可用成交量再决定是否进场：
   - **DoubleSide** 模式从做多开始，随后交替开多或开空。
   - **OneSide** 模式使用可重复的 `[0,5]` 伪随机整数：`1` 或 `4` 开多，`0` 或 `3` 开空，`2` 或 `5` 跳过当前 K 线。策略启动或重置时序列会重新开始。
4. 下单手数在最小与最大值之间均匀随机，并会按照交易品种的最小变动手数进行对齐。

## 参数

| 组别 | 名称 | 说明 |
|------|------|------|
| General | `Mode` | 方向逻辑：交替进场（`DoubleSide`，`0`）或随机过滤（`OneSide`，`1`）。 |
| Lot Settings | `MinVolume` / `MaxVolume` | 随机生成手数的范围。 |
| Protection | `TakeProfitPoints` | 止盈距离（价格步长）。 |
| Protection | `StopLossPoints` | 止损距离（价格步长）。 |
| Protection | `TrailingStartPoints` | 激活跟踪止损所需的盈利距离。 |
| Protection | `TrailingGapPoints` | 跟踪止损相对当前价格的距离。 |
| Filters | `MaxSpreadPoints` | Level1 最大允许点差（价格步长）。零值禁止所有新入场；bid/ask 不可用时，正值允许使用 K 线回退。 |
| Filters | `SlippagePoints` | 预期滑点，仅作参考。 |
| Risk Management | `MoneyRiskMode` | 固定金额（`FixedMoney`，`0`）或组合价值百分比（`BalancePercentage`，`1`）。 |
| Risk Management | `RiskValue` | 风险阈值（货币金额或百分比，取决于模式）。 |
| General | `TradeComment` | 入场订单的注释；平仓订单会附加触发原因。 |
| General | `CandleType` | 用于驱动策略的 K 线类型。 |

## 备注

- Level1 报价用于改进点差和清算价格计算。若双边报价不可用，正的点差上限允许 K 线回退；`MaxSpreadPoints = 0` 始终禁止入场。
- 保护条件只在 K 线完成后检查。盈利达到 `TrailingStartPoints + TrailingGapPoints` 个价格步长后启动跟踪，并保持 `TrailingGapPoints` 的距离。
- `FixedMoney` 将 `RiskValue` 解释为账户货币金额；`BalancePercentage` 将其解释为当前组合价值的百分比。
