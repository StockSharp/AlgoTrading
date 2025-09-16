# 外汇货币强弱策略

## 概述
**Forex Currency Power Strategy** 在 StockSharp 中复刻了 MetaTrader 的 *FOREX Currency Power* 仪表盘。它通过组合每个货币的四个主要货币对的归一化动量，测量五个主要货币（EUR、USD、GBP、CHF、JPY）的相对强弱。随后策略比较交易品种的基准货币与计价货币的强弱差，从而捕捉“最强对抗最弱”的机会。

原始脚本只负责绘制面板，本移植版本提供了可直接执行的交易逻辑：当基准货币比计价货币强出设定阈值时开多头，当计价货币更强时反向做空；当强弱差收窄到退出阈值之下时平仓。所有委托均为市价委托，以便立即响应强弱信号。

## 来自 MQL 的核心思想
- 使用一分钟 K 线，只在 K 线收盘后进行计算。
- 每种货币的强度由四个带权货币对的平均值组成：
  - **EUR**：EURUSD、EURGBP、EURCHF、EURJPY
  - **USD**：EURUSD、GBPUSD、USDCHF、USDJPY（对 EURUSD 和 GBPUSD 取反）
  - **GBP**：EURGBP、GBPUSD、GBPCHF、GBPJPY（对 EURGBP 取反）
  - **CHF**：EURCHF、USDCHF、GBPCHF、CHFJPY（对 EURCHF、USDCHF、GBPCHF 取反）
  - **JPY**：EURJPY、USDJPY、GBPJPY、CHFJPY（全部取反）
- 通过计算收盘价在最近 *N* 根 K 线最高价与最低价区间中的位置，将每个货币对归一化到 0–100 的刻度，与 MetaTrader 自定义品种保持一致。

## 实现细节
- **指标栈**：每个货币对都绑定一个 `Highest` 与一个 `Lowest` 指标，共享相同的回看长度，并通过 `SubscribeCandles(...).BindEx(...)` 来满足高阶 API 的要求。
- **货币聚合**：每当某个货币对得到新的归一化值时，就重新计算对应货币的强度。对于权重为负的贡献，使用 `100 - value` 进行反向处理，保持 0–100 的量纲。
- **交易逻辑**：由 `Strategy.Security` 的 K 线驱动。策略等待所有货币强度都可用、确认允许交易后，比较基准货币与计价货币的强弱差。开仓前会取消挂单，必要时平掉反向仓位，然后再发出市价单。
- **日志输出**：每根主交易品种 K 线结束时记录一条简洁的货币强度汇总，方便在没有图形面板的情况下检查移植质量。

## 参数
| 名称 | 说明 | 默认值 | 可优化 |
| --- | --- | --- | --- |
| `CandleType` | 所有订阅所用的 K 线数据类型。 | 1 分钟周期 | 否 |
| `Lookback` | 计算最高/最低区间时使用的 K 线数量。 | 5 | 是（3 → 20，步长 1） |
| `EntryThreshold` | 触发入场的最小强弱差（基准减计价）。 | 15 | 是（5 → 30，步长 5） |
| `ExitThreshold` | 当绝对强弱差低于此值时平仓。 | 5 | 是（2 → 15，步长 1） |
| `BaseCurrency` | 做多方向所代表的货币 ISO 代码。 | `EUR` | 否 |
| `QuoteCurrency` | 做多方向所卖出的货币 ISO 代码。 | `USD` | 否 |
| `EURUSD`、`EURGBP`、`EURCHF`、`EURJPY`、`GBPUSD`、`USDCHF`、`USDJPY`、`GBPCHF`、`GBPJPY`、`CHFJPY` | 构建货币篮子所需的证券。 | *(必填)* | 否 |

## 使用提示
1. 启动前需指定全部 10 个货币对以及实际交易品种，`BaseCurrency` 与 `QuoteCurrency` 应与 `Strategy.Security` 的 ISO 代码一致（例如交易 EURUSD 时选择 `EUR`/`USD`）。
2. 策略需要等待每个货币对至少积累 `Lookback` 根 K 线后才会开始发出信号。在此热身阶段内，强弱表未完成，交易逻辑保持空闲。
3. 阈值均以 0–100 的强弱刻度表示，建议让 `ExitThreshold` 低于 `EntryThreshold`，以免在入场水平附近来回反复。
4. 因为使用市价单入场，策略在启动时调用 `StartProtection()`，用户还可以通过 `Strategy.Volume` 等常规属性控制仓位规模和风险。

## 移植说明
- 使用 StockSharp 的高阶 API (`SubscribeCandles().BindEx`) 取代了原脚本的计时器循环与手动数组处理。
- 只有当指标值 `IsFinal` 为真时才进行计算，保证未完成的 K 线不会污染 0–100 的刻度。
- MetaTrader 的图形面板被结构化日志和自动化交易替代，以符合 StockSharp 策略模板的要求。

