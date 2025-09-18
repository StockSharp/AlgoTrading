# 回测交易助手面板策略

## 概述
**Backtesting Trade Assistant Panel Strategy** 源自 MetaTrader 4 专家顾问 *Backtesting Trade Assistant Panel V1.10*，原版在测试器窗口中绘制按钮与输入框，允许交易者在回测阶段手动调整手数、止损、止盈，并立即发送 BUY/SELL 市价单。迁移到 StockSharp 后，图形界面被策略参数与公开方法替代，但功能完全等价，依旧侧重于“人工下单 + 自动挂保护单”的流程。

主要特性：

- 以参数形式维护下单手数、止损与止盈距离（单位为 MetaTrader “point”）。
- 通过 `ManualBuy()`、`ManualSell()` 方法随时触发多/空市价单。
- 在每次下单后自动根据点数距离调用 `SetStopLoss`、`SetTakeProfit` 添加保护性委托。
- 提供 `SetOrderVolume`、`SetStopLoss`、`SetTakeProfit` 等工具函数，对应 MT4 面板上的可编辑文本框，运行中亦可调整。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `OrderVolume` | 市价下单时使用的手数，同时同步到基础的 `Strategy.Volume`。 | `0.1` |
| `StopLossPips` | 止损距离（以 point 为单位）。设为 `0` 时不自动挂出止损单。 | `50` |
| `TakeProfitPips` | 止盈距离（以 point 为单位）。设为 `0` 时不自动挂出止盈单。 | `100` |
| `MagicNumber` | 保留自原始 EA 的标识符，方便扩展或日志使用，StockSharp 本身不会读取。 | `99` |

## 手动操作
原版靠按钮触发动作，StockSharp 版本改为公开方法：

- `SetOrderVolume(decimal volume)` —— 同步下单手数并写入 `Strategy.Volume`。
- `SetStopLoss(decimal pips)` / `SetTakeProfit(decimal pips)` —— 动态调整止损/止盈点数，和 MT4 文本框的含义一致。
- `ManualBuy()` —— 按当前手数发送买入市价单，并基于合约信息将点数距离换算成价格差，随后调用 `SetStopLoss`、`SetTakeProfit`。
- `ManualSell()` —— 发送卖出市价单，逻辑与 `ManualBuy()` 对称。
- `CloseAllPositions()` —— 立即平掉所有持仓，对应测试中手动“flatten”的需求。

换算点值时沿用 MT4 的惯例：对于报价保留 5 位或 3 位小数的品种，`PriceStep` 会乘以 10 视为一个 point；其他品种直接使用 `PriceStep`。若行情缺失相关元数据，则退化为 `0.0001`，确保行为一致。

## 行为说明
- 策略订阅 Level1 行情以获取最新买卖价，若不可用则退回到最近成交价，再去挂保护单。
- 本策略不生成自动化信号，定位仍是“人工辅助执行器”。
- `MagicNumber` 仅为兼容字段，若需要进一步分类或记录，可在自定义扩展中引用。
- 在调用 `ManualBuy()`/`ManualSell()` 之前可以随时修改止损、止盈与手数，完全模拟原始面板的交互流程。

## 与原 EA 的差异
- 图形界面被参数和方法取代，所有功能通过程序调用即可完成。
- MT4 `OrderSend` 中固定 50 point 的滑点限制未迁移，StockSharp 的 `BuyMarket`/`SellMarket` 不提供对应参数，必要时请在外部风控或撮合层处理。
- 保护单通过 StockSharp 的高层 API (`SetStopLoss`/`SetTakeProfit`) 生成，更符合框架约定。

## 使用建议
1. 在 StockSharp 中配置好交易品种、投资组合及连接后启动策略。
2. 通过参数面板或方法调整 `OrderVolume`、`StopLossPips`、`TakeProfitPips`。
3. 需要进场时调用 `ManualBuy()` 或 `ManualSell()`，策略会自动挂出相应的保护单。
4. 使用 `CloseAllPositions()` 可以在回测或实时演练中快速平仓。
