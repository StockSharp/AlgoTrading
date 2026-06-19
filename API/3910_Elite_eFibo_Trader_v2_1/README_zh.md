# Elite eFibo Trader v2.1 策略

## 概述
Elite eFibo Trader v2.1 复刻了 MetaTrader 专家顾问，在单一方向上按斐波那契数列叠加仓位，并为所有挂单共用同一个保护性止损。移植到 StockSharp 后保持了原有行为：首笔市价单启动后，会根据 `LevelDistancePips` 间隔批量放置止损挂单，每当新的层级成交，就按斐波那契权重增加仓位。一旦共享止损被触发或浮动利润达到 `MoneyTakeProfit`，策略会立即平掉整个篮子。

该算法是刻意的单向结构。将 `OpenBuy` 设为 `true`（同时把 `OpenSell` 设为 `false`）可运行多头版本，反之则运行空头版本。任意时刻只会存在一个阶梯，完全复制 MQL4 脚本的一次性循环逻辑。

## 数据要求
- 订阅逐笔成交流以获取最新成交价，用于放置阶梯挂单、更新共享止损以及计算货币止盈。
- 依赖品种元数据（`PriceStep`、`StepPrice`、`VolumeStep`）将 MetaTrader 中的点值和手数转换为交易所价格与数量。

## 阶梯构建流程
1. 当没有持仓且允许交易时，策略检查方向开关。必须只有 `OpenBuy` 或 `OpenSell` 其中一个为 `true`，否则不会启动阶梯。
2. 第一个斐波那契层级以市价成交；其余层级按 `LevelDistancePips * pipSize` 的距离放置止损挂单，基准价为阶梯启动时记录的市价。
3. 每个层级的下单量来自 `Level1Volume` … `Level14Volume` 参数，并会根据品种的 `VolumeStep` 进行归一化。
4. 所有层级共用同一止损偏移量：`StopLossPips * pipSize`。每当有新层级成交时会生成初始止损，随后会自动收紧，使所有持仓共享距离最近的保护价格。

## 止损管理
- 每个成交层级都会保存自己的入场价与基于点值计算出的初始止损。
- 在每笔成交事件中，策略会重新计算所有持仓的止损，并向最紧的价格对齐（多头取最高止损，空头取最低止损），以模拟 MetaTrader 中频繁的 `OrderModify` 调整。
- 当最新成交价穿越任何共享止损时，策略会撤销剩余挂单，并用市价立即平掉整个篮子。

## 资金管理
- 未实现盈亏按照品种的 `PriceStep` 与 `StepPrice` 计算，确保现金目标与 MetaTrader 中的 `OrderProfit()` 一致。
- 当浮动利润达到或超过 `MoneyTakeProfit` 时，会即时平掉所有仓位并取消挂单。
- 若 `TradeAgainAfterProfit` 设为 `false`，在达到现金目标后策略会保持空闲，直到手动重新启动。

## 参数
| 名称 | 说明 |
| ---- | ---- |
| `OpenBuy` | 允许构建多头阶梯（必须与 `OpenSell` 互斥）。 |
| `OpenSell` | 允许构建空头阶梯（必须与 `OpenBuy` 互斥）。 |
| `TradeAgainAfterProfit` | 在篮子达到货币止盈后是否重新开始交易。 |
| `LevelDistancePips` | 相邻止损挂单之间的 MetaTrader 点数距离。 |
| `StopLossPips` | 每个成交层级的保护性止损点数距离。 |
| `MoneyTakeProfit` | 平掉整个篮子的货币止盈目标。 |
| `Level1Volume` … `Level14Volume` | 各斐波那契层级的下单量，设为 0 可跳过对应层级。 |

## 实现细节
- 点值换算遵循 MetaTrader 规则：报价带有 3 或 5 位小数时，真实点值为 `PriceStep * 10`。
- 启动时调用一次 `StartProtection()`，启用 StockSharp 内置的安全检查。
- 共享止损逻辑确保所有持仓保持同步；一旦出现更紧的止损，就会应用到每个活动层级。
- 当阶梯没有持仓时会自动清理挂单，对应 MQL 代码中反复执行的 `subCloseAllPending()`。

## 使用建议
- 请确保品种已正确配置 `PriceStep`、`StepPrice` 和 `VolumeStep`，否则点值换算和货币止盈都会失准。
- 平均加仓系统会迅速放大敞口，实盘前务必核对交易所的数量限制和保证金要求。
- 将 `TradeAgainAfterProfit` 设为 `false` 可复现原始 EA 中在盈利后停止交易的“一次性”模式。
