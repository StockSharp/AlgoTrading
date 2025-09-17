# iVIDyA Simple 策略

## 概述
该策略是 MetaTrader 专家顾问 **“iVIDyA Simple”** 的 StockSharp 高层 API 移植版。策略只交易一个品种，通过 Chande 动量振荡器（CMO）驱动的可变指数动态平均线（VIDYA）捕捉趋势。当最新完成的 K 线与经过偏移的 VIDYA 发生交叉时，策略按突破方向开立市价单，并可选地附加止损和止盈。

## 交易逻辑
1. 按照参数 `CandleType` 订阅指定周期的 K 线。
2. 将周期为 `CmoPeriod` 的 CMO 绑定到 K 线序列。其绝对值用于动态调整 VIDYA 的平滑系数，基础系数与原版一样为 `2 / (EmaPeriod + 1)`。
3. 每根收盘 K 线都会执行以下步骤：
   - 按 `AppliedPrice` 选择用于计算的价格（收盘价、开盘价、中价等）。
   - 根据自适应系数更新 VIDYA。
   - 保留 VIDYA 历史值，以复刻 MetaTrader 指标的 `ma_shift` 参数效果。
4. 将当前 K 线与向前偏移 `MaShift` 根的 VIDYA 比较：
   - 若开盘价在 VIDYA 下方、收盘价在 VIDYA 上方，则产生**买入**信号。
   - 若开盘价在 VIDYA 上方、收盘价在 VIDYA 下方，则产生**卖出**信号。
5. 开仓前会先平掉相反方向的仓位，使结果头寸等于设定交易量。
6. 如果止损或止盈距离大于零，则在每次进场后调用 `SetStopLoss` 和 `SetTakeProfit`。

这完全复刻了原始 EA：仅在新柱上触发、通过 CMO 与 EMA 构建 VIDYA，并用点值表示止损/止盈距离。

## 参数
| 名称 | 默认值 | 说明 |
|------|--------|------|
| `Volume` | `1` | 基础下单手数。策略在反手时会自动对冲现有仓位。 |
| `StopLossPoints` | `150` | 止损距离（价格步长）。设为 `0` 可关闭。 |
| `TakeProfitPoints` | `460` | 止盈距离（价格步长）。设为 `0` 可关闭。 |
| `CmoPeriod` | `15` | 控制 VIDYA 自适应权重的 CMO 周期。 |
| `EmaPeriod` | `12` | 定义 VIDYA 基础平滑系数的 EMA 周期。 |
| `MaShift` | `1` | VIDYA 向前偏移的已完成 K 线数量，对应 MetaTrader 的 `ma_shift`。 |
| `AppliedPrice` | `Close` | VIDYA 使用的价格类型（`Close`、`Open`、`High`、`Low`、`Median`、`Typical`、`Weighted`）。 |
| `CandleType` | `TimeSpan.FromMinutes(5)` | 用于所有计算与信号的 K 线类型/周期。 |

## 其他说明
- 止损和止盈通过高层 API (`SetStopLoss`/`SetTakeProfit`) 管理，而原始 MQL 代码需要手动检查冻结与最小距离。
- 策略只处理已完成的 K 线，从而严格遵守“新柱执行”规则。
- VIDYA 历史会自动截断，即使 `MaShift` 很大也不会占用过多内存。
- 根据项目要求，代码中的所有注释均使用英文。
