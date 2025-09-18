# CorrTime 策略
[English](README.md) | [Русский](README_ru.md)

CorrTime 策略是对 MetaTrader 同名专家顾问的移植。它通过比较收盘价与时间顺序之间的相关性来捕捉动能的增强或衰减。所有决策都基于已经完成的 K 线，并叠加三重过滤条件：

1. **波动过滤**：布林带宽度必须处于设定的区间内，从而避开极端震荡或极端平静的时段。
2. **趋势过滤**：平均趋向指数（ADX）必须高于阈值，否则忽略当前信号。
3. **相关性触发器**：可选择皮尔逊、斯皮尔曼、肯德尔或费希纳相关系数来衡量价格与时间的联系，当系数快速改变时触发交易决策。

原始脚本用于 EURUSD 的 H1 图表。StockSharp 版本保留全部可调参数，默认值与来源一致（1 小时 K 线、费希纳相关、逆势模式）。

## 运行流程

1. 订阅所选 `CandleType`，等待 K 线收盘。
2. 在新 K 线上更新布林带与 ADX 数值。
3. 若满足以下任一条件，则跳过当前 K 线：
   - 布林带宽度（换算成点值）不在 `[BollingerSpreadMin, BollingerSpreadMax]` 之间。
   - ADX 低于 `AdxLevel`。
   - K 线的开盘时间不在 `[EntryHour, EntryHour + OpenHours]` 交易窗口内（支持跨日）。
4. 维护收盘价历史，分别对 `CorrelationRangeTrend` 和 `CorrelationRangeReverse` 的窗口计算相关系数。为了检测阈值的真正突破，每次都会得到最近三个相关性值，逻辑与原始 MQL 库中的缓冲区完全一致。
5. 趋势模式（`TradeMode = TrendFollow` 或 `Both`）：
   - **做多**：相关系数位于 `CorrLimitTrendBuy` 以下，并在最新一根 K 线上突破该阈值。
   - **做空**：相关系数位于 `-CorrLimitTrendSell` 以上，并在最新一根 K 线上跌破该阈值。
6. 逆势模式（`TradeMode = Reverse` 或 `Both`）：
   - **做多**：相关系数位于 `-CorrLimitReverseBuy` 以下，并在最新一根 K 线上突破该阈值。
   - **做空**：相关系数位于 `CorrLimitReverseSell` 以上，并在最新一根 K 线上跌破该阈值。
7. 如果多空信号同时出现，则互相抵消。
8. 当 `CloseTradeOnOppositeSignal` 为真时，策略会在开新仓前先平掉反向持仓。
9. 下单量使用 `Volume`，并受到 `MaxOpenOrders` 限制，净持仓不会超过 `Volume * MaxOpenOrders`。
10. 通过 `StartProtection` 管理风险：止盈、止损和拖尾止损距离以点为单位，再换算为价格距离。

## 参数说明

| 参数 | 含义 |
|------|------|
| `CandleType` | 计算信号所使用的 K 线类型。 |
| `CloseTradeOnOppositeSignal` | 出现反向信号时是否先行平仓。 |
| `EntryHour`, `OpenHours` | 每日交易窗口，`OpenHours = 0` 表示仅开放一个小时。 |
| `BollingerPeriod`, `BollingerDeviation` | 布林带的周期和标准差倍数。 |
| `BollingerSpreadMin`, `BollingerSpreadMax` | 允许的布林带宽度（点值）。 |
| `AdxPeriod`, `AdxLevel` | ADX 周期及其阈值。 |
| `TradeMode` | 选择顺势、逆势或两者兼顾。 |
| `CorrelationRangeTrend`, `CorrelationRangeReverse` | 相关性计算的窗口长度。 |
| `CorrelationType` | 相关系数类型（皮尔逊、斯皮尔曼、肯德尔、费希纳）。 |
| `CorrLimitTrendBuy`, `CorrLimitTrendSell` | 顺势信号的阈值。 |
| `CorrLimitReverseBuy`, `CorrLimitReverseSell` | 逆势信号的阈值。 |
| `TakeProfitPips`, `StopLossPips`, `TrailingStopPips` | 止盈、止损、拖尾距离（点）。 |
| `MaxOpenOrders` | 最大合计下单次数，等同于 `Volume * MaxOpenOrders` 的仓位上限。 |

## 实用提示

- 点值依据合约的小数位数自动推导：5 位或 3 位小数会乘以 10，与原始 MQL 程序中 `Point` 与 `MULT` 的处理完全一致。
- 相关性判断需要至少 `窗口 + 2` 根收盘 K 线，热身阶段不会触发信号。
- 策略仅使用收盘数据，避免了盘中噪音，并严格对应 MetaTrader 中以 `iTime`/`iClose` 生成信号的方式。
- 若与其他策略组合使用，请额外设置组合层面的风险限制，原始专家顾问同样限制了账户中的总订单数量。
