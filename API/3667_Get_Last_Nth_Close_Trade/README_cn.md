# 获取倒数第 N 笔平仓交易策略

## 概述
**获取倒数第 N 笔平仓交易策略** 还原了 MetaTrader 4 智能交易程序 `Get_Last_Nth_Close_Trade.mq4` 的行为。原版脚本会持续扫描终端的历史订单，按照平仓时间倒序排列，然后输出距最新成交第 *n* 笔交易的详细信息。本 C# 版本依托 StockSharp 的高级策略 API，在托管环境中实现相同的流程。

策略监听由当前策略实例提交的订单，收集所有状态达到 `Done` 的订单记录，按最新平仓时间排序，并输出用户所设索引对应的交易字段。当没有符合筛选条件的平仓交易时，策略会在日志中给出说明。

## 行为映射
| MetaTrader 4 概念 | StockSharp 实现 |
| --- | --- |
| `OrdersHistoryTotal` 与手动循环 | 通过订单状态回调维护内部的平仓订单列表。 |
| 可选品种筛选 (`ENABLE_SYMBOL_FILTER`) | 启用后仅记录 `Strategy.Security` 对应的订单。 |
| 可选魔术号码筛选 (`ENABLE_MAGIC_NUMBER`) | 启用后把订单备注中的数字视作魔术号码，并与参数对比。 |
| 按 `OrderCloseTime` 排序 | 使用订单最近变更时间进行倒序排序。 |
| `Comment()` 输出多行摘要 | 通过 `AddInfo` 将格式化文本写入策略日志。 |

## 参数
- **Enable Magic Number** – 打开或关闭魔术号码筛选，筛选时会读取订单备注中的数字。
- **Enable Symbol Filter** – 仅在所选品种上统计平仓订单。
- **Magic Number** – 当启用魔术号码筛选时，要匹配的整数值。
- **Trade Index** – 从最新平仓开始的零基索引，决定要显示第几笔交易。

所有参数都以 `StrategyParam<T>` 公开，可直接用于 StockSharp 的优化器。

## 输出
每当平仓列表发生变化或列表为空时，策略都会输出一段与原版 EA 类似的多行文本：

```
ticket 123456
symbol EURUSD@FXCM
lots 1
openPrice 1.2345
closePrice 1.2350
stopLoss 0
takeProfit 0
comment 1234
type Market
orderOpenTime 2024-03-01T10:15:00+00:00
orderCloseTime 2024-03-01T11:00:00+00:00
profit 0.0005
```

只有当内容发生变化时才会刷新文本，以避免刷屏。

## 限制
- StockSharp 没有 MetaTrader 式的魔术号字段，因此需要下单组件在订单备注中写入与参数一致的整数。
- 盈亏、止损和止盈数值依赖券商适配器提供的数据。如果缺失，对应字段会保持为零。
- 该移植版仅跟踪策略运行期间提交的订单，启动前历史成交无法回溯。

## 使用提示
1. 选择合约和投资组合后正常启动策略。
2. 若要复刻 MetaTrader 的魔术号筛选，请确保订单备注包含与 **Magic Number** 参数相同的整数。
3. 调整 **Trade Index** 以选择自最新平仓起第几笔交易（0 表示最新，1 表示次新，以此类推）。
4. 在策略日志中查看格式化摘要。

## 文件结构
- `CS/GetLastNthCloseTradeStrategy.cs` – 策略的 C# 实现。
- `README.md` – 英文说明。
- `README_cn.md` – 本文件（中文说明）。
- `README_ru.md` – 俄文说明。

