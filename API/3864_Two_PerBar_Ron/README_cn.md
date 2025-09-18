# TwoPerBar Ron 策略
[English](README.md) | [Русский](README_ru.md)

## 概述
原始的 MetaTrader 专家顾问 “TwoPerBar” （作者 Ron Thompson）在每根新 K 线开始时会同时开出 **两笔市价单**：一多一空。当其中任意一条腿达到固定的利润目标（MQL 代码中的 `ProfitMade * Point`）时立即平仓；下一根 K 线开始前会强制关闭剩余仓位，再重新建立新的对冲组合。如果上一根 K 线结束时仍有持仓，则按照 `LotLimit` 设定的上限将手数加倍。StockSharp 版本通过高级策略 API、Level 1 报价以及对两条腿的显式跟踪来复刻这一节奏。

## 交易流程
1. **识别新 K 线** – 通过 `SubscribeCandles(CandleType)` 订阅指定的蜡烛序列，当收到 `CandleStates.Finished` 的蜡烛时，就像 MetaTrader 中 `Time[0]` 改变一样意味着新的一根开始。
2. **检查利润** – 持续监听 Level 1 报价（最佳买价/卖价）。一旦最佳报价距离建仓价达到设定目标，就用 `SellMarket` 或 `BuyMarket` 平掉对应的腿。
3. **强制平仓** – 在每根新 K 线开始时，先把所有剩余腿全部市价平掉，对应 MQL 中循环调用 `OrderClose` 的逻辑。
4. **手数递增** – 如果上一轮仍有仓位未能在 K 线内关闭，则把手数乘以 `VolumeMultiplier`（默认 2）；否则回到 `BaseVolume`。之后会根据品种的 `VolumeStep`、`Security.MaxVolume` 等限制进行规范化和截断。
5. **重建对冲** – 分别调用 `BuyMarket` 和 `SellMarket` 下单。每条腿都会记录目标手数、实际成交量以及加权平均成交价，以便后续精确计算收益。

## 风险与资金管理
- **类马丁加仓** – 当上一轮未全部平仓时自动放大手数，完全复制原策略的马丁结构；若两条腿都在同一根内获利出场，则重置为基础手数。
- **单腿止盈** – `ProfitTargetPoints` 是 MQL 参数 `ProfitMade` 的等价物。它与价格步长相乘后，与实时 bid/ask 比较来决定何时平仓。
- **符合交易所约束** – `NormalizeVolume` 会根据 `VolumeStep` 与 `MinVolume` 调整手数，超出范围时会回退到可交易的数值。
- **显式对冲记录** – 策略内部维护一份腿列表，因为大多数 StockSharp 投资组合只提供净头寸。要获得与 MetaTrader 相同的效果，需要交易通道支持对冲账户。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 分钟蜡烛 | 触发新一轮流程的主要时间框架。 |
| `BaseVolume` | `decimal` | `0.1` | 全新周期使用的初始手数。 |
| `VolumeMultiplier` | `decimal` | `2` | 当上一根 K 线留下持仓时应用的乘数。 |
| `MaxVolume` | `decimal` | `12.8` | 手数递增的硬上限。 |
| `ProfitTargetPoints` | `decimal` | `19` | 以“点”为单位的利润目标，乘以价格步长后与 bid/ask 比较。 |

## 与 MQL 版本的差异
- 使用 `SubscribeLevel1()` 获取实时报价，而不是直接访问全局 `Bid`/`Ask` 变量，但核心逻辑保持一致。
- 通过 `BuyMarket`、`SellMarket` 等高级方法下单，所有交易所细节由 StockSharp 负责处理。
- 手数会自动匹配 `VolumeStep`、`MinVolume`、`MaxVolume` 等约束；原脚本只操作原始 `double` 数值。
- 策略内部维护对冲腿数据；如果连接的经纪商采用净额制度，可能会自动对冲掉仓位，请确认账户支持双向持仓。

## 使用建议
- 请先将 `BaseVolume` 调整为品种允许的最小交易单位，否则规范化后会跳过下单。
- 结合品种的价格步长设置 `ProfitTargetPoints`，过大的目标很难在一根 K 线内达成。
- 由于策略同时开多空仓，建议先在模拟或允许对冲的账户上测试。
- `OnStarted` 会把蜡烛和成交绘制到图表上 (`DrawCandles`, `DrawOwnTrades`)，方便实时监控。
