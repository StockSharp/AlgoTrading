# WE TRUST 通道策略

## 概述
**WE TRUST 通道策略** 是 MetaTrader 5 专家顾问 “WE TRUST” 的 StockSharp 高层 API 移植版本。策略围绕一条线性加权移动平均线（LWMA）构建，并使用标准差带形成价格通道。当收盘价突破通道后，系统预期价格将回归均值并在通道内反向开仓。信号反转、可选的反向持仓平仓以及基于点值的风险参数完全复刻原始 EA。

## 交易逻辑
1. 订阅配置的K线类型（默认 1 小时）并在所选价格类型上计算两个指标：
   - 带有可调周期和位移的线性加权移动平均线（LWMA）。
   - 拥有独立周期和位移的标准差指标，用于构建波动区间。
2. 通过证券的 `PriceStep` 将所有以点（pip）为单位的距离转换为绝对价差。对于 5 位或 3 位报价，会将最小价位乘以 10，以匹配 MetaTrader 中对点值的定义。
3. 计算上下轨：`LWMA ± StdDev ± ChannelIndentPips`，其中偏移量均以价格单位表示。
4. 只处理收盘完成的K线。当指定价格低于下轨时产生**买入**信号；当高于上轨时产生**卖出**信号。
5. 若启用 **ReverseSignals**，则将买卖方向互换。若启用 **CloseOpposite**，则在产生新信号前先平掉反向持仓。
6. 当仓位为空或与信号方向一致时，以设置的手数提交市价单。

## 风险管理
- **StopLossPips** 与 **TakeProfitPips** 通过 `StartProtection` 转换为绝对价差并自动管理止损/止盈，填 `0` 可禁用相应保护。
- **TrailingStopPips** 与 **TrailingStepPips** 控制以点值计算的移动止损，二者使用与止损相同的点值换算逻辑。
- 所有离场操作均使用市价单执行，以保持与 MQL5 实现一致。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `OrderVolume` | 每次下单使用的交易量。 | `0.1` |
| `StopLossPips` | 以点表示的止损距离（0 表示关闭止损）。 | `40` |
| `TakeProfitPips` | 以点表示的止盈距离（0 表示关闭止盈）。 | `60` |
| `TrailingStopPips` | 移动止损的初始距离，单位为点。 | `10` |
| `TrailingStepPips` | 每次调整移动止损之间的点数间隔。 | `10` |
| `MaPeriod` | 线性加权移动平均的周期。 | `60` |
| `MaShift` | 移动平均线的偏移条数。 | `0` |
| `StdDevPeriod` | 标准差的计算周期。 | `50` |
| `StdDevShift` | 标准差的偏移条数。 | `0` |
| `SignalBarOffset` | 回看多少根已完成K线进行信号判定。 | `1` |
| `ChannelIndentPips` | 在上下轨外额外增加的缓冲点数。 | `1` |
| `ReverseSignals` | 是否反转买卖信号。 | `false` |
| `CloseOpposite` | 产生新信号前是否先平掉反向仓位。 | `false` |
| `AppliedPrice` | 指标所使用的价格类型。 | `Weighted` |
| `CandleType` | 策略请求的数据K线类型。 | `1 小时` 时间框架 |

## 备注
- 策略依赖于证券的 `PriceStep` 元数据；若交易所未提供，将依次回退到 `Security.Step` 或 `1`。
- 仅提供 C# 版本，按照要求暂不包含 Python 实现。
- 策略仅处理完整收盘的K线，不会累计未完成的柱状数据。
