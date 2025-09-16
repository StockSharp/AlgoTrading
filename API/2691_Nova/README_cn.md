# Nova 策略

## 概述
- 由 MetaTrader 5 的 "Nova" 专家顾问转换而来，用于跟踪固定秒数内的价格动量。
- 通过 `CandleType` 参数选择任意蜡烛类型，并仅在蜡烛完结时执行逻辑。
- 使用 Level1 数据跟踪最优买价和卖价，并保存 `SecondsAgo` 秒之前的报价作为比较基准。
- 当前一根蜡烛收阳且当前卖价相对基准卖价上升至少 `StepPips` 时建立**多头**仓位。
- 当前一根蜡烛收阴且当前买价相对基准卖价下降至少 `StepPips` 时建立**空头**仓位。
- 当止损或止盈参数大于零时，利用 StockSharp 的保护机制自动放置止损/止盈。
- 若出现亏损（触发止损），下一笔交易的手数乘以 `LossCoefficient`；若盈利退出，则手数恢复为 `BaseVolume`。

## 参数
- `SecondsAgo` – 用于比较的历史报价与当前时刻之间的秒数。
- `StepPips` – 突破过滤阈值（以点为单位）；根据品种的最小报价单位自动换算为价格增量（3/5 位小数的品种乘以 10）。
- `BaseVolume` – 初始下单手数；会按照交易所的最小变动、最小/最大手数进行归一化处理。
- `StopLossPips` – 止损距离（点）；为 0 时表示不放置止损。
- `TakeProfitPips` – 止盈距离（点）；为 0 时表示不放置止盈。
- `LossCoefficient` – 发生亏损后用于放大下一次下单手数的倍数。
- `CandleType` – 用于生成信号的蜡烛类型（时间框、tick、range 等）。

## 其他说明
- 为完整复刻 MT5 行为，策略需要 Level1 (最优买/卖) 数据；若不可用则回退为蜡烛的收盘价。
- 手数归一化过程会遵循 `Security.VolumeStep`、`Security.MinVolume` 与 `Security.MaxVolume` 的限制，避免无效订单。
- 价格换算基于 `Security.PriceStep` 与 `Security.Decimals`，可适配 4/5 位外汇品种及其他市场品种。
- 本策略不提供 Python 版本。
