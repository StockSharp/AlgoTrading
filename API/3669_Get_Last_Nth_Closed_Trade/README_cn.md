# Get Last Nth Closed Trade 策略

## 概述
**Get Last Nth Closed Trade** 策略复刻了 MetaTrader 专家顾问的功能：扫描账户历史，并输出从最近开始数起的第 _n_ 笔已平仓交易。StockSharp 版本不会提交新的委托，它仅监控策略自身的成交，不断维护最近 100 笔平仓信息，并在每次平仓后记录所选索引的快照。

## 工作流程
1. `OnNewMyTrade` 收到的每一笔成交都会按照可选的品种过滤和策略标识过滤进行验证。
2. 内部的虚拟持仓管理器会在加仓时更新平均开仓价，在部分平仓时扣减剩余数量。
3. 当持仓减少时，策略构建 `ClosedTradeInfo`，其中包含方向、价格、时间戳、标识符、成交量以及基于平均开仓价的简单收益估算。
4. 最多保留 100 条平仓记录。每次新增平仓时，都会以接近 MetaTrader `Comment` 的多行格式将目标索引的记录写入日志。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `EnableStrategyIdFilter` | 设为 `true` 时，仅处理 `StrategyId` 与 `StrategyIdFilter` 匹配的成交；当过滤字符串为空时使用当前策略标识。 | `false` |
| `StrategyIdFilter` | 当启用过滤时需要匹配的策略标识。 | `""` |
| `EnableSecurityFilter` | 设为 `true` 时，只处理 `Strategy.Security` 对应品种的成交。 | `false` |
| `TradeIndex` | 需要输出的平仓记录（从 0 开始）索引。 | `0` |

## 注意事项与限制
- StockSharp 的成交信息不包含止损和止盈价格，因此报告中省略相关字段。
- 收益按照价格差乘以平仓数量计算；如需考虑佣金或滑点，请自行调整。
- 持仓追踪支持加仓和部分平仓：通过维护平均开仓价来保持准确度。当方向反转时，会先结清旧仓，再用剩余数量建立新的虚拟持仓。
- 日志格式与 MetaTrader 多行 `Comment` 相似，便于复制到外部工具或脚本中进一步处理。
