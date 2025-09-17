# MultiTrader 货币强度策略 (3253)

## 概述
本策略是公开 MQL 面板“MultiTrader”（代码 #24786）的 StockSharp 高级 API 版本。原始 EA 是一套人工交易面板，用来显示八大主要货币的相对强弱、在极端情况下弹出提醒，并提示潜在的交易货币对。移植后的 StockSharp 策略自动化了相同的分析流程，并在需要时对最强与最弱货币构成的货币对执行下单操作。

策略对每个货币对计算当前收盘价在当期 K 线区间中的百分比位置，通过加权平均获得 AUD、CAD、CHF、EUR、GBP、JPY、NZD、USD 的强弱分值。当某个货币的强度高于买入阈值，同时另一种货币低于卖出阈值时，系统会推荐由这两种货币组成的货币对。如果该货币对存在于已配置的市场列表中，则可以自动执行市价单。

## 货币强度模型
百分比分值的计算公式为：

```
percent = 100 * (Close - Low) / (High - Low)
```

每种货币都基于 7 个交叉汇率，完全复刻 MQL 版本。若该货币在货币对中位于报价货币一侧，则使用 `100 - percent` 来反向处理：

| 货币 | 组成 | 
| --- | --- |
| AUD | AUDJPY、AUDNZD、AUDUSD、100-EURAUD、100-GBPAUD、AUDCHF、AUDCAD |
| CAD | CADJPY、100-NZDCAD、100-USDCAD、100-EURCAD、100-GBPCAD、100-AUDCAD、CADCHF |
| CHF | CHFJPY、100-NZDCHF、100-USDCHF、100-EURCHF、100-GBPCHF、100-AUDCHF、100-CADCHF |
| EUR | EURJPY、EURNZD、EURUSD、EURCAD、EURGBP、EURAUD、EURCHF |
| GBP | GBPJPY、GBPNZD、GBPUSD、GBPCAD、100-EURGBP、GBPAUD、GBPCHF |
| JPY | 100-AUDJPY、100-CHFJPY、100-CADJPY、100-EURJPY、100-GBPJPY、100-NZDJPY、100-USDJPY |
| NZD | NZDJPY、100-GBPNZD、NZDUSD、NZDCAD、100-EURNZD、100-AUDNZD、NZDCHF |
| USD | 100-AUDUSD、USDCHF、USDCAD、100-EURUSD、100-GBPUSD、USDJPY、100-NZDUSD |

策略只使用已经收盘的 K 线；每当收到新的完成 K 线数据时，就会刷新所有货币的强度。

## 交易与提醒
1. 当八种货币的强度都可用时，策略会按照强弱排序输出快照日志。
2. 如果最强货币的数值 **≥ BuyLevel** 且最弱货币的数值 **≤ SellLevel**，则生成交易建议。
3. 系统优先寻找“强势货币做基准、弱势货币做报价”的直接货币对；若不存在则尝试反向配对，最后回退到与 USD 组合的货币对。
4. 推荐的货币对与方向会被记录到日志中。如果 `EnableAutoTrading` 为真且 `OrderVolume` 大于零，将在该方向上发送市价单。若持有反向仓位，订单会自动加量以平掉旧仓再建立新仓。

策略会记忆最近一次推荐的货币对和方向，避免在行情未脱离阈值区间时重复提示。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `Universe` | 需要分析的 `Security` 列表，建议包含 28 个主要交叉盘。 | 必填 |
| `CandleType` | 强度计算所使用的 K 线类型（可选日线、周线、月线等）。 | 日线 |
| `BuyLevel` | 认定货币过强的百分比阈值。 | 90 |
| `SellLevel` | 认定货币过弱的百分比阈值。 | 10 |
| `EnableAutoTrading` | 是否启用自动下单。 | false |
| `OrderVolume` | 自动交易时的下单数量。 | 1 |
| `SymbolPrefix` | 交易所或券商使用的前缀（例如 `m.`）。 | "" |
| `SymbolSuffix` | 交易所或券商使用的后缀（例如 `.FX`）。 | "" |

## 配置步骤
1. **设置交易标的。** 将 28 个主要货币对加入策略的 Universe。代码需与标准货币对名称一致，如 `EURUSD`。如券商添加前后缀，可在参数中填写。
2. **选择周期。** 设置 `CandleType`。日线、周线、月线分别对应原始面板的三种模式。
3. **调整阈值。** 根据交易风格修改 `BuyLevel` 与 `SellLevel`，控制发出信号的极端程度。
4. **自动交易（可选）。** 将 `EnableAutoTrading` 设为 true，并指定 `OrderVolume`。如果只需要提示，可保持默认。

## 迁移说明
- 原 MQL 版本的大量图形界面被移除，所有输出均通过策略日志给出。
- 推送、邮件、弹窗等提醒未被移植，统一使用 `LogInfo` 日志。
- 自动止损/止盈功能未实现，需要结合 StockSharp 的保护模块或其他风险控制方案。
- MQL 中的 DES 授权函数被删除。

## 使用建议
- 在提供实时与历史 K 线数据的连接器环境中运行本策略。
- 可配合图表部件展示被推荐的货币对及其 K 线走势。
- 建议结合 `StartProtection` 或独立的风险管理策略设定全局止损、止盈。

## 测试注意事项
- 确保数据源能够提供所选周期的完整 K 线，策略会忽略未收盘的柱。
- 若 Universe 中缺少某些货币对，对应货币的强度无法计算，策略也不会产生信号。
- 回测时请确保 Universe 在整个测试期间保持一致，避免强度出现断档。
