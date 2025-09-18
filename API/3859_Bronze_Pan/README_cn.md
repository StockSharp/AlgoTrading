# Bronze Pan 策略

该策略将 MetaTrader 4 的 "Bronzew_pan" 专家顾问移植到 StockSharp。它只针对一支证券，在 K 线收盘后运行，通过自定义的 DayImpuls 振荡器配合 Williams %R 与 CCI 来捕捉动能反转。

## 工作流程

1. 订阅设定的 K 线类型，并以相同周期计算 DayImpuls、Williams %R 以及 CCI。
2. 分别记录多头与空头持仓，模拟原机器人支持对冲的行为。
3. 当浮动盈亏达到 `ProfitTarget` 或跌破 `LossTarget` 时立即平掉全部持仓。
4. 当 DayImpuls 高于 `DayImpulsShortLevel` 且走低，同时 Williams %R 高于 `WilliamsLevelUp`、CCI 大于 `CciLevel` 时开空。
5. 当 DayImpuls 低于 `DayImpulsLongLevel` 且走高，同时 Williams %R 低于 `WilliamsLevelDown`、CCI 小于 `-CciLevel` 时开多。
6. 若浮动盈亏突破 `PredBand` 区间，则按 `LotMultiplier` 倍放大下单量执行强制反手，以还原 MetaTrader 中的紧急加仓逻辑。
7. 分别监控多头与空头篮子的止损/止盈，将以点数表示的距离换算为价格差。
8. 当账户余额低于 `MinimumBalance` 或多空篮子均已持仓时不再开新单。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 基础下单手数。 | `0.1` |
| `LongStopLossPips` | 多头篮子止损（点）。 | `0` |
| `ShortStopLossPips` | 空头篮子止损（点）。 | `0` |
| `LongTakeProfitPips` | 多头篮子止盈（点）。 | `0` |
| `ShortTakeProfitPips` | 空头篮子止盈（点）。 | `0` |
| `IndicatorPeriod` | DayImpuls、Williams %R、CCI 共用周期。 | `14` |
| `CciLevel` | 确认极值所需的 CCI 绝对阈值。 | `150` |
| `WilliamsLevelUp` | 进空所需的 Williams %R 水平。 | `-15` |
| `WilliamsLevelDown` | 进多所需的 Williams %R 水平。 | `-85` |
| `DayImpulsShortLevel` | DayImpuls 空头确认阈值。 | `50` |
| `DayImpulsLongLevel` | DayImpuls 多头确认阈值。 | `-50` |
| `ProfitTarget` | 触发全部平仓的浮动盈利。 | `500` |
| `LossTarget` | 触发全部平仓的浮动亏损。 | `-2000` |
| `PredBand` | 触发强制反手的盈亏带宽。 | `100` |
| `LotMultiplier` | 强制反手时的下单量倍数。 | `30` |
| `MinimumBalance` | 继续交易所需的最低账户余额。 | `3000` |
| `CandleType` | 使用的 K 线周期。 | `15m` |

## 说明

- DayImpuls 振荡器复制了原版“蜡烛实体转点值并做两次 EMA 平滑”的计算过程。
- 止损/止盈为 0 时表示禁用对应保护。
- 仅在蜡烛状态为 `Finished` 时才会执行交易逻辑。
