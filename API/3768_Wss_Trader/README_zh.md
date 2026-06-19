# Wss Trader

移植自 forex-instruments.info 上的 MetaTrader 4 专家顾问 “Wss_trader”。原版策略结合 Camarilla 反转水平与经典枢轴点，只要价格在伦敦时段内突破预设区间，就在每根 K 线上至多开一笔仓位。

## 策略逻辑

1. 每个交易日开始时读取上一交易日的最高价、最低价和收盘价并构建枢轴阶梯：
   - `Pivot = (High + Low + Close) / 3`
   - `Long entry = Pivot + Metric × point`
   - `Short entry = Pivot − Metric × point`
   - `Long stop = Short entry`
   - `Short stop = Long entry`
   - 止盈目标沿用 MetaTrader 中的 `Close ± (High − Low) × 1.1 / 2` 公式，并保留原始安全限制。
2. 只有在 `Start Hour` 与 `End Hour`（含边界）之间允许交易，离开时间窗口会立即平掉现有仓位。
3. 当收盘价向上穿越多头入场线（当前收盘价 ≥ 水平且前一收盘价 < 水平）时，以设定手数买入一次，并记录预先计算的止损与止盈；该根 K 线不会再次入场。空头信号采用对称条件。
4. 当浮动盈利至少达到 `Trailing Points` 个价格步长时，止损会向盈利方向跟随至相同距离，且永不后退。

## 参数

| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `Working Candle` | 用于日内计算的主要 K 线类型。 | `15 Minute` |
| `Daily Candle` | 读取上一交易日数据的 K 线类型。 | `1 Day` |
| `Start Hour` | 开始允许交易的小时（0-23）。 | `8` |
| `End Hour` | 停止接受新交易的小时（0-23）。 | `16` |
| `Metric Points` | 从枢轴到突破水平的距离（价格步长）。 | `20` |
| `Trailing Points` | 移动止损距离（价格步长，`0` 表示关闭）。 | `20` |
| `Order Volume` | 对应原始 `lots` 参数的下单手数。 | `0.1` |

## 说明

- 时间窗口结束后会立即平仓，以保持与原 EA 相同的行为。
- 移动止损基于已完成的 K 线计算；由于本移植版本使用收盘数据，无法完全再现逐笔推进的效果。
- 每根 K 线仅允许一次入场，对应 MQL 版本中的 `tenb` 标志。
