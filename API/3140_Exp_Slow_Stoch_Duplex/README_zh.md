# Exp Slow Stoch Duplex 策略

本策略基于 MetaTrader 5 专家顾问 **Exp_Slow-Stoch_Duplex**，在 StockSharp 高级 API 中重写。它同时跟踪两个独立时间框架上的慢速随机振荡器，协同生成多头和空头信号，并通过保护性订单复现原始 EA 的止盈止损管理。

## 交易逻辑

- **多头模块**
  - 在 `LongCandleType` 时间框架上计算慢速随机指标。
  - 对 %K 和 %D 应用所选的平滑方式，并按照 `LongSignalBar` 指定的条数进行偏移。
  - 当 %K 上穿 %D（`previousK <= previousD` 且 `currentK > currentD`）时买入。
  - 当 %K 再次跌破 %D（`currentK < currentD`）时平掉现有多头。
- **空头模块**
  - 在 `ShortCandleType` 时间框架上计算慢速随机指标。
  - 当 %K 下穿 %D（`previousK >= previousD` 且 `currentK < currentD`）时做空。
  - 当 %K 再次上穿 %D（`currentK > currentD`）时平掉现有空头。
- 所有订单均以市价单提交。下单数量为 `TradeVolume` 加上当前净持仓的绝对值，从而在反向开仓前先对冲掉旧头寸。
- 使用 `StartProtection` 附加点值单位的止盈和止损距离，以模拟 MT5 订单参数。

## 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `LongCandleType` | `DataType` | 8 小时 K 线 | 多头随机指标的时间框架。 |
| `LongKPeriod` | `int` | 5 | 多头随机指标的 %K 周期。 |
| `LongDPeriod` | `int` | 3 | 多头随机指标的 %D 平滑周期。 |
| `LongSlowing` | `int` | 3 | 随机指标内部的额外平滑系数。 |
| `LongSignalBar` | `int` | 1 | 计算交叉信号时向前回溯的条数。 |
| `LongSmoothingMethod` | `SmoothingMethod` | `Smoothed` | 对 %K/%D 进行二次平滑的方法（None、Simple、Exponential、Smoothed、Weighted）。 |
| `LongSmoothingLength` | `int` | 5 | 多头二次平滑的长度。 |
| `LongEnableOpen` | `bool` | `true` | 是否允许开多头。 |
| `LongEnableClose` | `bool` | `true` | 是否允许平多头。 |
| `ShortCandleType` | `DataType` | 8 小时 K 线 | 空头随机指标的时间框架。 |
| `ShortKPeriod` | `int` | 5 | 空头随机指标的 %K 周期。 |
| `ShortDPeriod` | `int` | 3 | 空头随机指标的 %D 平滑周期。 |
| `ShortSlowing` | `int` | 3 | 随机指标内部的额外平滑系数。 |
| `ShortSignalBar` | `int` | 1 | 计算空头交叉信号时的回溯条数。 |
| `ShortSmoothingMethod` | `SmoothingMethod` | `Smoothed` | 对空头 %K/%D 进行二次平滑的方法。 |
| `ShortSmoothingLength` | `int` | 5 | 空头二次平滑的长度。 |
| `ShortEnableOpen` | `bool` | `true` | 是否允许开空头。 |
| `ShortEnableClose` | `bool` | `true` | 是否允许平空头。 |
| `TradeVolume` | `decimal` | 0.1 | 基础下单手数。 |
| `TakeProfitPoints` | `decimal` | 2000 | 止盈距离，点值表示。 |
| `StopLossPoints` | `decimal` | 1000 | 止损距离，点值表示。 |

## 备注

- `SmoothingMethod` 提供了 StockSharp 内置的均线平滑，以近似原始 JJMA 平滑。如需关闭此步骤，可选择 `None`。
- 多头与空头模块互不干扰，可通过布尔参数独立启用或禁用。
- StockSharp 采用净持仓模型，因此当方向反转时会先平掉相反持仓，再建立新方向。
