# 标准差通道策略

## 概览
该策略是 MetaTrader 专家顾问 **Standard Deviation Channel** 的 StockSharp 版本。它基于线性加权移动平均线（LWMA）构建波动率通道，只在通道突破与主要趋势方向一致时开仓。入场信号由动量强度和 MACD 交叉确认，离场部分结合固定目标、保本跳动以及跟踪止损。

## 指标与信号
- **标准差通道**：使用 LWMA 作为中线并乘以可配置的偏差倍数。做多要求上轨向上倾斜，做空要求下轨向下倾斜。
- **趋势过滤器**：同一时间框架上的快、慢 LWMA。做多条件为 `LWMA_fast > LWMA_slow`，做空条件相反。
- **动量过滤器**：14 周期 Momentum 指标，最近三次读数中至少有一次偏离 100 的幅度达到设定阈值。
- **MACD 过滤器**：经典的 12/26/9 组合。多头需要 `MACD ≥ signal`，空头需要 `MACD ≤ signal`。

## 仓位管理
- **仓位规模**：由 `TradeVolume` 参数控制，反向信号会先平掉反向持仓再建立新仓。
- **止盈与止损**：以点数表示，通过合约的 `PriceStep` 换算成价格。一旦蜡烛的最高/最低触及目标价或止损价，策略立即以市价离场。
- **保本跳动**：浮动盈利达到 `BreakEvenTriggerPips` 后，将止损移动到开仓价并加上（或减去）`BreakEvenOffsetPips`。
- **跟踪止损**：盈利超过 `TrailingStartPips` 后，止损会以 `TrailingStepPips` 的距离跟随价格，逐步锁定收益。
- **通道回落退出**：如果收盘价重新回到通道内且斜率反向，提前平仓。

## 参数
| 名称 | 说明 |
| --- | --- |
| `CandleType` | 指标所用的主时间框架。 |
| `TradeVolume` | 基础下单手数。 |
| `TrendLength` | 定义通道中线的 LWMA 周期。 |
| `DeviationMultiplier` | 通道宽度所用的标准差倍数。 |
| `FastMaLength` / `SlowMaLength` | 趋势过滤器的快、慢 LWMA 周期。 |
| `MomentumPeriod` | Momentum 指标周期。 |
| `MomentumThreshold` | 最近三次动量与 100 的最小偏差。 |
| `TakeProfitPips` / `StopLossPips` | 固定止盈/止损距离，通过 `PriceStep` 转换。 |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | 控制何时以及如何执行保本移动。 |
| `TrailingStartPips` / `TrailingStepPips` | 跟踪止损的启动条件与步长。 |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD 参数。 |
| `MaxPositionUnits` | 最大净持仓，防止过度杠杆。 |

## 使用建议
1. 仅在具备有效 `PriceStep` 的品种上运行，以保证点数换算正确。
2. 通过调整 `TrendLength` 与 `DeviationMultiplier` 适配不同波动率环境。
3. 若想提高交易频率，可降低 `MomentumThreshold` 或缩短动量/MACD 周期。
4. 跟踪止损基于蜡烛收盘计算，盘中未封闭的冲击不会触发移动。

## 与原始 EA 的差异
- 原 MT4 脚本依赖图形对象读取通道斜率，并包含加码和权益保护等复杂资金管理。移植版本保留斜率判断，但将风险控制简化为固定手数和 `MaxPositionUnits` 限制。
- 所有离场动作都在蜡烛收盘时以市价执行，因为 StockSharp 不支持 MT4 式的订单修改接口。
- 邮件与推送通知改为 `AddInfoLog` 日志，便于在示例环境中复现。
- 账户层面的权益止损未实现，重点放在单笔交易的止损与保本机制上。

## 免责声明
本策略仅供学习研究使用，任何实盘部署前请务必在历史及模拟环境中充分验证。
