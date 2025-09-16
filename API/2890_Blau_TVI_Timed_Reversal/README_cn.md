# Blau TVI 定时反转策略

## 概述
- 由 `MQL/21014` 中的 MetaTrader 5 专家顾问 **Exp_BlauTVI_Tm.mq5** 转换而来。
- 在 C# 中复刻了 Blau Tick Volume Index（三阶段平滑的成交量指标）计算过程，并保留了所有关键参数。
- 当平滑后的 TVI 斜率发生方向改变时产生反转信号，可选地仅在指定的交易时段内开仓。
- 提供以价格点数表示的止损和止盈保护，与原始 EA 的风控逻辑一致。

## Blau TVI 指标计算
原始 EA 调用自定义指标 `BlauTVI.ex5`：它利用 tick 成交量估算涨跌单数，并经过多重平滑。移植后的逻辑如下：

1. **原始涨跌单数**
   - `UpTicks = (Volume + (Close - Open) / PriceStep) / 2`
   - `DownTicks = Volume - UpTicks`
   - StockSharp 的蜡烛数据没有单独提供 tick 数，因此使用蜡烛的总成交量作为近似。
2. **第一阶段平滑**：分别对 `UpTicks` 与 `DownTicks` 使用所选移动平均类型（EMA/SMA/SMMA/WMA/JMA）和 `Length1` 进行平滑。
3. **第二阶段平滑**：对第一阶段输出再次平滑，长度为 `Length2`。
4. **TVI 计算**：`TVI = 100 * (Up2 - Down2) / (Up2 + Down2)`。
5. **第三阶段平滑**：将 TVI 以 `Length3` 再平滑一次，以降低噪音。

策略会保存少量最近的 TVI 值，以复现原脚本中 `SignalBar`（调用 `CopyBuffer` 时的偏移）带来的信号延迟效果。

## 交易逻辑
- **斜率检测**
  - 当 `SignalBar + 1` 的 TVI 低于 `SignalBar + 2` 的值，并且最新值再度高于 `SignalBar + 1`，判定为向上拐头，触发做多信号。
  - 当 `SignalBar + 1` 的 TVI 高于 `SignalBar + 2`，且最新值低于 `SignalBar + 1`，判定为向下拐头，触发做空信号。
- **持仓管理**
  - `EnableBuyOpen` 为真且当前仓位≤0时，根据做多信号买入；若存在空头仓位，会自动加仓量以平掉空单。
  - `EnableSellOpen` 为真且当前仓位≥0时，根据做空信号卖出；若存在多头仓位，会补足数量以反手。
  - `EnableBuyClose` 为真且斜率转为向下时，平掉多单。
  - `EnableSellClose` 为真且斜率转为向上时，平掉空单。
- **时间过滤**
  - `EnableTimeFilter` 为真时，仅在 [`StartHour`:`StartMinute`, `EndHour`:`EndMinute`] 时间段内开新仓；若时段跨越午夜（起始时间大于结束时间）也能正确处理。
  - 一旦当前时间离开设定区间，策略会立即按市价平掉已有仓位。
- **风控**
  - `StopLossPoints` 与 `TakeProfitPoints` 会乘以合约的 `PriceStep` 转换为绝对价格距离，传入 `StartProtection`。设置为 0 即关闭对应保护。

## 参数说明
| 参数 | 含义 |
|------|------|
| `Volume` | 每次开仓的基础数量；若需要反手，会自动加上已有反向仓位的绝对值。 |
| `CandleType` | 计算所用的蜡烛数据类型/时间框架，默认 4 小时。 |
| `MaType` | 三个平滑阶段共用的移动平均类型（EMA/SMA/SMMA/WMA/JMA）。 |
| `Length1`, `Length2`, `Length3` | 三个平滑阶段的长度。 |
| `SignalBar` | 信号偏移量，1 表示使用上一根已完成蜡烛，与原脚本一致。 |
| `EnableBuyOpen`, `EnableSellOpen` | 允许开多 / 开空。 |
| `EnableBuyClose`, `EnableSellClose` | 允许在斜率反转时平多 / 平空。 |
| `EnableTimeFilter` | 是否启用时间过滤。 |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | 交易时段的起止时间，支持跨夜。 |
| `StopLossPoints`, `TakeProfitPoints` | 以价格点数表示的固定止损/止盈距离，0 表示关闭。 |

## 实现细节
- 由于缺少逐笔 tick 信息，蜡烛成交量被用作 tick 成交量的近似，这与原版思路保持一致，同时兼容 StockSharp 数据源。
- 仅维护极少量的 TVI 历史值即可满足 `SignalBar` 的需求，避免了大型集合，符合仓库对内存使用的限制。
- 只有在合约存在有效 `PriceStep` 时才会根据点数计算止损止盈；否则调用 `StartProtection()` 但不设固定距离。
- 所有注释均为英文，代码缩进使用制表符，遵循根目录 `AGENTS.md` 的要求。

## 使用建议
1. 建议先使用默认的 H4 时间框架与 EMA 平滑，这与原始 EA 的参数一致。
2. 若希望在上一根蜡烛收盘立即反应，可将 `SignalBar` 调整为 0，但这会偏离原策略的延迟逻辑。
3. 针对交易时段不连续的品种，请务必配置时间过滤，以避免在流动性不足时进场。
4. 该策略保持固定下单量，如需更复杂的资金管理，可在组合层面叠加自定义逻辑。
