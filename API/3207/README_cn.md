# 3207 – MA 趋势策略

## 概述
**MA Trend Strategy** 使用 StockSharp 高级 API 重现 MetaTrader 专家顾问 *MA Trend.mq5*。策略跟随一条可以向前平移的线性加权移动平均线。当收盘价上穿平移后的均线时做多，下穿时做空，并提供与原 MQL 版本相同的止损、止盈和移动止损管理。

## 交易逻辑
1. 订阅设定的蜡烛类型（默认 1 分钟），根据选择的均线方法和价格源计算移动平均。
2. 将得到的均线数值按照 `MaShift` 参数向前平移若干已完成的 K 线。
3. 生成信号：
   - **做多** – 收盘价高于平移后的均线（当 `ReverseSignals` 为 `true` 时条件反向）。
   - **做空** – 收盘价低于平移后的均线（当 `ReverseSignals` 为 `true` 时条件反向）。
4. 应用持仓管理选项：
   - 若 `CloseOpposite=true`，在开仓前先平掉相反方向的持仓。
   - 若 `OnlyOnePosition=true` 且已有持仓，则阻止新的开仓。
5. 止损、止盈和移动止损的距离均以点（pip）为单位。当未实现盈利超过 `TrailingStopPips + TrailingStepPips` 时，移动止损才会推进，与 MQL 原版一致。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `OrderVolume` | `decimal` | `0.1` | 下单手数（合约数量）。 |
| `StopLossPips` | `int` | `50` | 止损点数，0 表示关闭固定止损。 |
| `TakeProfitPips` | `int` | `140` | 止盈点数，0 表示关闭固定止盈。 |
| `TrailingStopPips` | `int` | `15` | 移动止损的基础距离，0 表示禁用。 |
| `TrailingStepPips` | `int` | `5` | 更新移动止损前所需的额外盈利点数，启用移动止损时必须为正。 |
| `MaPeriod` | `int` | `12` | 移动平均的周期长度。 |
| `MaShift` | `int` | `3` | 移动平均向前平移的已完成 K 线数量。 |
| `MaMethod` | `MovingAverageKind` | `Weighted` | 移动平均的计算方式（Simple、Exponential、Smoothed、Weighted）。 |
| `AppliedPrice` | `AppliedPriceMode` | `Weighted` | 输入到均线的价格类型（Close、Open、High、Low、Median、Typical、Weighted）。 |
| `OnlyOnePosition` | `bool` | `false` | 是否限制同一时间只持有一笔仓位。 |
| `ReverseSignals` | `bool` | `false` | 是否反转买卖信号。 |
| `CloseOpposite` | `bool` | `false` | 是否在开仓前先平掉反向仓位。 |
| `CandleType` | `DataType` | `1 分钟` | 用于计算的蜡烛类型/时间周期。 |

## 说明
- Pip 大小会根据 3/5 位报价的品种自动调整，与 MetaTrader 的点值兼容。
- 若 `TrailingStopPips > 0` 而 `TrailingStepPips <= 0`，策略会在启动时抛出异常，以确保移动止损逻辑有效。
- 仅在蜡烛收盘后才进行指标计算和交易决策，便于回测和优化复现。
