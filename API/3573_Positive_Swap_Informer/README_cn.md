# 正向掉期监视策略

## 概述
**Positive Swap Informer Strategy** 是 MetaTrader 指标脚本 `Swap Informer` (MQL/41693) 的 StockSharp 版本。该策略会定期扫描可配置的行情观察列表，并记录所有当前提供正向多头或空头掉期的交易品种。它适用于希望监控套息机会或寻找提供正向隔夜利息工具的交易者。

策略本身不会发送订单。它仅用于信息通知：每次计时器触发时都会生成一份汇总报告写入策略日志，方便用户手动处理或结合其他自动化流程。

## 行为流程
1. 启动时，策略根据 `SymbolsList` 参数构建观察列表，并可选地包含主交易品种 `Strategy.Security`。
2. 对观察列表中的每个品种发起 Level1 订阅，以便交易连接器将掉期值填充到证券元数据中。
3. 使用 `RefreshInterval` 参数指定的周期启动计时器。每次触发时都会在 `Security.ExtensionInfo` 中查询相关的掉期字段。
4. 如果检测到正向掉期，会将结果按如下格式加入日志：
   ```
   <SecurityId>: Swap Long (Buy) = <value>
   <SecurityId>: Swap Short (Sell) = <value>
   ```
5. 如果未找到任何正向掉期，日志会输出 `Positive swap report: no symbols with a positive swap were found.`。

输出格式与原始 MetaTrader 脚本保持一致，方便复用已有的文本解析或通知流程。

## 参数说明
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `SymbolsList` | 需要额外监视的证券代码列表，使用逗号、分号、空格、制表符或换行分隔。 | `""` |
| `RefreshInterval` | 重新扫描的时间间隔。如果设置为小于等于 0，会自动回退为 1 秒。 | `00:00:10` |
| `IncludePrimarySecurity` | 为 `true` 时自动将主交易品种 `Strategy.Security` 加入观察列表。 | `true` |

## 数据要求
* 交易商或行情源必须通过 `Security.ExtensionInfo` 提供掉期信息。策略会查找 `SwapBuy`、`SwapLong`、`SwapBuyPoints`、`SwapLongPoints`、`SwapSell`、`SwapShort`、`SwapSellPoints` 与 `SwapShortPoints` 等键（大小写不敏感）。
* 如果数据源使用了其他键名，可在代码中的 `_longSwapKeys` 与 `_shortSwapKeys` 集合中添加别名。
* 策略不需要任何历史数据，完全基于元数据运行。

## 使用步骤
1. 将策略添加到 StockSharp 终端或算法宿主中，如平台需要可绑定投资组合。
2. （可选）在 `SymbolsList` 中填写需要监视的品种代码，必须与 `SecurityProvider` 中的标识一致。
3. 根据需要调整 `RefreshInterval`，默认值为 10 秒。
4. 启动策略。首次计时器触发后，日志会显示正向掉期列表或缺失提示。
5. 可在日志窗口中查看结果，或将日志转发到文件/通知系统。

## 注意事项
* 策略不会对掉期数值进行归一化。不同的行情源可能提供点值、货币金额或百分比，请根据经纪商说明解读。
* 某些连接器只有在出现首笔成交或特定交易会话事件后才会填充掉期信息，此时需要等待数据更新。
* 策略仅执行只读操作，可与其他交易策略并行运行，不会影响订单或持仓。
* 每次报告都会重新读取最新的元数据，不缓存上一次的结果，以免遗漏突发变化。

## 与 MetaTrader 脚本的对应关系
| MetaTrader 概念 | StockSharp 实现 |
|----------------|----------------|
| `SymbolsTotal(true)` | `SecurityProvider` 查询加上可选的主交易品种 |
| `SymbolInfoDouble(..., SYMBOL_SWAP_LONG/SHORT)` | 在 `Security.ExtensionInfo` 中查找预定义的掉期键 |
| `Comment()` 输出 | `LogInfo()` 日志 |
| `EventSetTimer(1)` | 由 `RefreshInterval` 控制的计时器（默认 10 秒） |

## 扩展建议
* 如果数据源使用额外的键名，可向 `_longSwapKeys` 或 `_shortSwapKeys` 添加新条目。
* 如需自定义通知，可在 `OnTimer` 中替换 `LogInfo`，例如推送到消息队列或邮件系统。
* 计时器逻辑封装在 `StartTimer`/`StopTimer` 中，可根据需求扩展为批量导出或节流控制。

## 测试建议
策略本身不会对市场造成影响，因此未提供单元测试。验证时可连接支持掉期数据的经纪商，运行策略并确认日志输出与预期一致。
