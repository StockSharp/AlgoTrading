# eKeyboardTrader 策略

## 概述
该策略使用 StockSharp 的高级 API 复刻了 MetaTrader 的 “eKeyboardTrader” 智能交易系统。原始脚本通过键盘快捷键提交手动市价单，并在图表上显示提示信息。移植到 StockSharp 后，交互入口改为策略参数，但手动交易流程、安全检查和保护单设置依旧与 MQL 版本保持一致。

## 交易逻辑
1. **Level1 订阅**——策略订阅证券的买一卖一报价，用于确认最新价格后再执行手动指令。
2. **手动指令**——三个布尔参数（`BuyRequest`、`SellRequest`、`CloseRequest`）对应原策略的 B、S、C 热键。只要某个参数被设为 `true`，策略就会执行对应的市价操作，并立即把参数重置为 `false`。
3. **频率限制**——两次指令之间至少间隔一秒，以防止重复提交，与 MQL 实现完全一致。
4. **仓位保护**——可选的止损/止盈距离以 MetaTrader 点数表示，通过 `Security.PriceStep` 转换为绝对价格。当任一保护距离大于零时，策略会调用 `StartProtection`，为之后的每一笔手动交易自动挂上保护单。
5. **滑点提示**——`SlippagePoints` 参数保留下来用于兼容，并在每次发送手动订单时写入日志，模拟原脚本的提示文本。

## 参数
| 参数 | 说明 |
|------|------|
| `OrderVolume` | 手动市价单的基础交易量。 |
| `StopLossPoints` | 止损距离（MetaTrader 点数，0 表示禁用）。 |
| `TakeProfitPoints` | 止盈距离（MetaTrader 点数，0 表示禁用）。 |
| `SlippagePoints` | 日志中显示的滑点容忍值。 |
| `BuyRequest` | 置为 `true` 时提交买入市价单（处理完毕后自动重置）。 |
| `SellRequest` | 置为 `true` 时提交卖出市价单（处理完毕后自动重置）。 |
| `CloseRequest` | 置为 `true` 时按市价平掉当前净头寸（处理完毕后自动重置）。 |

## 与 MQL 版本的差异
- 不再在图表上绘制提示文字或播放声音，所有动作都会写入日志。
- 保护单通过 `StartProtection` 管理，达到阈值时以市价单平仓，而不是修改单独的 MetaTrader 挂单。
- 键盘操作改为参数开关，宿主界面可以把按钮、脚本或热键映射到这些参数。
- MetaTrader 中详细的交易请求诊断被压缩成精简的日志记录。

## 使用说明
- 启动策略前必须先指定 `Security` 和 `Portfolio`，这与原脚本的前置检查一致。
- 手动指令在收到新的 Level1 数据时处理，如果市场暂时无报价，动作会延迟到下一笔报价。
- 在策略运行期间修改 `StopLossPoints` 或 `TakeProfitPoints` 后，需要重启策略以重新初始化保护模块，这与原始实现只启动一次保护的逻辑一致。
