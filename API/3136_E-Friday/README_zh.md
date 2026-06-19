# E-Friday 策略

## 概述
- 将 MetaTrader 专家顾问 `E-Friday.mq5` 移植到 StockSharp 的高级 API。
- 仅在 **H1 及以下** 的时间框架工作，若选择更大的周期则给出警告并保持空仓。
- 采用反趋势思路：看跌蜡烛触发做多，看涨蜡烛触发做空。
- 每逢周五完全停止交易，继承原始程序的周末保护机制。
- 支持交易时间窗口，并可在收盘时间后强制平仓。

## 交易逻辑
1. 每根蜡烛收盘时检查当前交易所时间：
   - 若为周五则直接退出；
   - 若当前小时小于 `StartHour` 则等待；
   - 若启用 `UseCloseHour` 且当前小时大于 `EndHour`，立即平掉所有仓位并忽略新信号。
2. 允许交易时根据上一根完成的蜡烛判断方向：
   - `Open > Close`（实体向下）→ 准备开多；
   - `Open < Close`（实体向上）→ 准备开空；
   - 开盘价等于收盘价时不执行操作。
3. 开启新方向前会先平掉现有仓位，保证始终只有一个净头寸。

## 仓位管理
- **手数**：来自 `TradeVolume` 参数，通过 `BuyMarket` / `SellMarket` 下单。
- **止损/止盈**：以点（pip）为单位。根据 `Security.PriceStep` 计算点值，若品种有 3 或 5 位小数则乘以 10，与原始 EA 的处理完全一致。
- **移动止损**：当价格朝有利方向运行 `TrailingStopPips + TrailingStepPips` 点后启动，将止损调整至 `现价 ± TrailingStopPips`。
- 使用蜡烛的最高价和最低价判断出场：
  - 多单：最低价触及止损或最高价达到止盈即平仓；
  - 空单：最高价触及止损或最低价达到止盈即平仓。
- 当 `UseCloseHour = true` 且当前小时超过 `EndHour` 时，通过市价单强制平仓。

## 参数
| 参数 | 说明 |
| --- | --- |
| `CandleType` | 订阅的蜡烛类型，需要提供正的 `TimeSpan`，建议不超过一小时。 |
| `TradeVolume` | 下单手数，必须为正。 |
| `StopLossPips` | 初始止损距离（点）。为 0 表示不设置止损。 |
| `TakeProfitPips` | 初始止盈距离（点）。为 0 表示不设置止盈。 |
| `TrailingStopPips` | 移动止损的基础距离（点）。 |
| `TrailingStepPips` | 每次收紧止损所需的额外收益（点），启用移动止损时必须大于 0。 |
| `StartHour` | 允许开仓的起始小时（交易所时间）。 |
| `UseCloseHour` | 是否在结束时间后强制平仓。 |
| `EndHour` | 停止交易并平仓的小时（交易所时间）。 |

## 实现说明
- 通过 `SubscribeCandles` + `Bind` 获取数据，方便后续扩展指标准备。
- 启动时校验移动止损设置：开启移动止损时必须提供正的 `TrailingStepPips`。
- 点值换算完全复刻 MQL 版本的逻辑（3/5 位小数时乘以 10），确保风险参数一致。
- StockSharp 实现以收盘蜡烛判断止损/止盈，而原版在每个 tick 上判断，因此可能出现轻微的出入，但总体决策保持一致。
- `CloseActivePosition` 明确在交易窗口结束后平仓；原脚本中存在相同意图，但由于提前 `return` 而未执行，这里做了修正。
- 使用 `AddInfoLog` 与 `AddWarningLog` 输出跳过交易的原因，方便在界面中查看。
