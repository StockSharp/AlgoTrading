# 二元期权品种扫描策略

## 概述
本策略复现了 `Binary Option Symbol.mq4` 指标中 `OnInit` 函数的行为，目的在于
从可用的证券列表中找出符合二元期权特征的合约。在 MetaTrader 中，该脚本会
遍历终端内的全部交易品种，并打印出 `MODE_PROFITCALCMODE == 2` 且
`MODE_STOPLEVEL == 0` 的符号。转换到 StockSharp 之后，我们通过检查数据源
为 `Security` 填充的元数据来完成同样的过滤。

## 策略逻辑
1. 读取 `Symbols` 参数，并按照逗号、分号或空白字符拆分成一组待检查的代码。
2. 利用 `SecurityProvider.LookupById` 为每个代码解析出对应的 `Security` 对象。
3. 从 `Security.ExtensionInfo` 字典中提取 `ProfitCalcMode` 与 `StopLevel` 两个
   字段。
4. 如果两个字段分别等于 `ProfitCalcMode` 与 `StopLevel` 参数的取值（默认分别
   为 `2` 和 `0`），则通过 `AddInfoLog` 记录该品种属于二元期权候选。
5. 如果缺失任何字段，则通过 `AddDebugLog` 给出调试信息，方便检查数据供应商
   是否提供了足够的元数据。
6. 当 `Symbols` 为空时，策略会退回到主策略的 `Security` 属性，只分析该单一品
   种。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `Symbols` | 需要扫描的证券标识，支持逗号、分号或空白分隔。留空时将只分析主策略的 `Security`。 | *(空)* |
| `ProfitCalcMode` | 期望的利润计算模式值，对应 MQL 中的 `MODE_PROFITCALCMODE` 常量。 | `2` |
| `StopLevel` | 期望的最小止损距离，对应 MQL 中的 `MODE_STOPLEVEL` 常量。 | `0` |

## 使用说明
- 请确认连接的交易所适配器会在 `ExtensionInfo` 中提供 `ProfitCalcMode` 与
  `StopLevel`。若两项任意缺失，策略将无法判断该品种是否满足条件。
- 若代码无法通过 `SecurityProvider.LookupById` 解析，策略会输出警告。请检查
  代码拼写或确保该标的已经在连接会话中加载。
- 策略仅打印日志，不会发送任何委托，可放心在仿真或真实环境中执行。
- 使用结果可以帮助你维护二元期权自选列表，或进一步配置其他自动化策略。

## 与原始 MQL 实现的差异
- MetaTrader 能直接访问 `SymbolsTotal` 来遍历全部品种，而 StockSharp 需要
  用户通过 `Symbols` 参数显式提供扫描列表。该转换保留了相同的筛选逻辑，并
  在未提供列表时自动退回到主策略的 `Security`。
- MQL 通过 `MarketInfo` 查询品种属性；在 StockSharp 中我们读取
  `Security.ExtensionInfo` 来完成相同工作，符合高阶 API 的使用建议。
- 为了提高可观测性，额外添加了信息和调试日志，说明每个品种被接受或拒绝的
  原因。

## 快速上手
1. 在 StockSharp 终端或自定义宿主中连接到目标交易场所。
2. 创建 `BinaryOptionSymbolScannerStrategy` 实例。
3. 将 `Symbols` 设置为 `EURUSD, NAS100, XAUUSD, EURGBP` 等候选列表。
4. 启动策略，并在日志中查看诸如 `Binary option symbol detected: EURUSD` 的输出。

通过该策略生成的报告，你可以快速确认当前数据源提供了哪些二元期权品种，便
于后续策略或风控配置。
