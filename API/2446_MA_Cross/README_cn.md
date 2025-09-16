# MA 交叉策略

## 概述
本策略在 StockSharp 平台中重现了 MetaTrader 5 指标顾问“MA Cross”（文件 `MA Cross.mq5`）的逻辑。系统跟踪两条可配置的移动平均线，只要快线穿越慢线就会发送市价单。为了与原脚本保持一致，代码提供了移动平均类型、应用价格以及指标位移等完整的参数。

## 策略逻辑
1. 根据 `CandleType` 参数订阅单一的蜡烛流。
2. 在每根已完成蜡烛上计算快慢两条移动平均线。移动平均可选择简单、指数、平滑或线性加权方式，输入值可选择收盘价、开盘价、最高价、最低价、中价、典型价或加权价。
3. 保存最近的指标值，并考虑配置的位移，使交叉检测能够使用前几根蜡烛的结果。
4. 当快线从慢线下方穿越至上方时触发看多信号；当快线从慢线上方跌破至下方时触发看空信号。
5. 仅在两条移动平均均已形成且策略处于在线状态时下单。多头信号会平掉现有空头仓位并按 `OrderVolume` 开多头；空头信号则反向操作。

策略仅处理完成的蜡烛，不会提前使用未完结数据。调用 `StartProtection()` 以启用 StockSharp 的保护逻辑，对未平仓头寸进行监控。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `FastPeriod` | 3 | 快速移动平均周期。 |
| `SlowPeriod` | 13 | 慢速移动平均周期。 |
| `FastMethod` | Simple | 快线的移动平均计算方式（简单、指数、平滑或线性加权）。 |
| `SlowMethod` | LinearWeighted | 慢线的计算方式。 |
| `FastPriceType` | Close | 快线使用的应用价格（收盘、开盘、最高、最低、中价、典型价、加权价）。 |
| `SlowPriceType` | Median | 慢线使用的应用价格。 |
| `FastShift` | 0 | 快线向左移动的已完成蜡烛数量。 |
| `SlowShift` | 0 | 慢线向左移动的已完成蜡烛数量。 |
| `OrderVolume` | 1 | 每笔市价单的交易量。 |
| `CandleType` | 1 分钟 | 参与计算的蜡烛类型。 |

所有参数均通过 `StrategyParam` 注册，可以在 StockSharp 中进行优化测试。

## 交易规则
- **做多：** 快线在位移修正后的慢线上方完成金叉。如果策略持有空头，将发送一笔买单同时平空并建立多头；若无持仓，则仅买入 `OrderVolume`。
- **做空：** 快线在慢线下方完成死叉。若当前持有多头，将通过单笔卖单反向；若空仓，则直接卖出 `OrderVolume`。
- **不叠加仓位：** 已持仓的方向重复出现同向信号时被忽略，直到出现反向交叉。
- **执行方式：** 使用 `BuyMarket` 与 `SellMarket` 发出市价单，策略本身不设置止损或止盈，风险控制可由其他模块补充。

## 转换说明
- `MovingAverageMethod` 枚举对应 MetaTrader 的 `MODE_SMA`、`MODE_EMA`、`MODE_SMMA` 与 `MODE_LWMA`，分别映射到 `SimpleMovingAverage`、`ExponentialMovingAverage`、`SmoothedMovingAverage` 和 `WeightedMovingAverage`。
- 应用价格计算严格遵循 MetaTrader 的 `ENUM_APPLIED_PRICE` 定义，通过蜡烛的高、低、收盘价组合得到中价、典型价和加权价。
- `FastShift` 与 `SlowShift` 的实现保留了原脚本逻辑：策略缓存指标值，并在比较时引用位移后的历史数值。
- 持仓管理逻辑与原版一致：出现反向信号时先平掉现有仓位，再在同一根蜡烛上建立新仓位。
