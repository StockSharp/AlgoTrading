# Pinball Machine 策略

## 概览
本策略是 MetaTrader 4 专家顾问 `Pinball_machine.mq4` 的 StockSharp 翻译版本。原始 EA 会在每个到来的报价上生成随机整数，只要其中一对数值相同就立刻开仓。移植后的 StockSharp 版本保留了这种类似“弹球机”的随机玩法：在所选时间框架的每根已完成K线上执行两组随机抽签，只要对应的一对数字相等，就分别开出做多或做空的市价单。止损和止盈距离同样在每次评估时重新随机化，从而还原原程序那种不可预测的弹跳效果。

## 交易逻辑
- 按 `CandleType` 参数订阅K线，并等待每根K线收盘。
- 对于每根完成的K线，在区间 `[0, RandomMaxValue]` 内生成四个均匀分布的整数。第一对用于潜在的多单，第二对用于潜在的空单。
- 再额外生成两组整数，分别位于 `MinStopLossPoints`/`MaxStopLossPoints` 和 `MinTakeProfitPoints`/`MaxTakeProfitPoints` 之间，用于决定保护性止损与止盈的距离（以价格步长为单位），该距离在多空方向上共用。
- 如果第一、第二个随机数相同，就按 `TradeVolume` 的手数提交一张市价买单；如果第三、第四个随机数相同，就提交同样手数的市价卖单。两个条件可以在同一根K线上同时触发，与原始 MQL 程序中买卖互不干扰的行为完全一致。
- 若本次抽取的保护距离大于零，立即调用 `SetStopLoss` 与 `SetTakeProfit` 挂出止损和止盈。距离会按交易品种的 `PriceStep` 进行换算，对应 MetaTrader 中以 `Point` 为单位的写法。

## 订单与风控
- 启动时调用 `StartProtection()`，让 StockSharp 自动管理附加的保护性订单。
- 每次进场都会先计算下单后的净持仓量（`Position ± TradeVolume`），再将该值传给 `SetStopLoss` 与 `SetTakeProfit`，从而在同向多笔交易并存时依旧能合并管理止损与止盈。
- 如果止损或止盈的最小/最大距离设为 0 或负值，本次评估会跳过对应的保护单。

## 参数说明
| 参数 | 说明 |
|------|------|
| `TradeVolume` | 每次随机进场的下单数量（手数或合约数）。 |
| `CandleType` | 触发随机抽签的K线时间框架；时间越短越贴近原始的逐笔行情执行方式。 |
| `RandomMaxValue` | 随机整数的上限（含）。数值越大，命中相同数字的概率越低，进场频率越少。 |
| `MinStopLossPoints` | 随机生成的止损距离下限（按价格步长计）。 |
| `MaxStopLossPoints` | 随机生成的止损距离上限。 |
| `MinTakeProfitPoints` | 随机生成的止盈距离下限。 |
| `MaxTakeProfitPoints` | 随机生成的止盈距离上限。 |
| `RandomSeed` | 伪随机数发生器的种子。为 0 时根据当前时间播种，其它数值则产生可复现的序列。 |

## 实现要点
- 原 EA 以逐笔报价驱动；StockSharp 版本改为在K线收盘时运行，因为高阶 API 以时间序列事件为核心。可将 `CandleType` 设置为极短周期（如 1 秒或交易所报价K线）以还原原版的快速节奏。
- 止损和止盈距离在每次评估时只生成一次，并同时用于多单和空单，完全遵循原始脚本的写法。
- 请确保交易品种已设置正确的 `PriceStep`，否则以点数表示的保护距离需要手动调整。
