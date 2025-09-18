# Ichimoku 动量 MACD 策略

## 摘要
- **类型**：动量确认的趋势跟随。
- **周期**：可配置（默认 15 分钟 K 线）。
- **指标**：Ichimoku（Tenkan/Kijun）、线性加权均线、动量、MACD。
- **止损/止盈**：通过 `StartProtection` 设置的点数止盈和止损（可选）。

## 策略说明
该策略复刻了 MetaTrader 专家顾问 “Ichimoku”（目录 `MQL/23469`）的核心判定逻辑。它只在前一根
收盘 K 线上完成所有指标的计算，并在下一根 K 线开盘时检查以下四个条件：

1. **Ichimoku 排列** —— 多头需要 Tenkan 高于 Kijun，空头则相反。
2. **LWMA 趋势过滤** —— 快速线性加权均线必须在慢速均线上方（做多）或下方（做空）。
3. **动量强度** —— 最近三根 K 线中至少有一根的动量指标与 100 的偏离值大于阈值。
4. **MACD 确认** —— MACD 主线相对信号线的位置与方向一致（同号且主线在信号线外侧）。

当四个条件全部看多且当前未持有多单时，策略买入设定手数，同时对冲掉已有的空头仓位；当条件
全部看空时执行对称的卖出操作。方向反转信号也会用来平掉已有持仓，即使没有启用保护性止损/止盈，
策略也能获得确定性的离场规则。

风险控制通过 `StartProtection` 完成，可为止盈和止损指定点数距离，设置为 0 表示关闭该保护。

## 参数概览
| 参数 | 说明 |
|------|------|
| `FastMaPeriod` | 用于趋势过滤的快速线性加权均线长度。 |
| `SlowMaPeriod` | 慢速线性加权均线长度。 |
| `MomentumPeriod` | 动量指标的计算周期。 |
| `MomentumThreshold` | 最近三根 K 线中动量偏离 100 的最小要求。 |
| `MacdFastPeriod` | MACD 快速 EMA 周期。 |
| `MacdSlowPeriod` | MACD 慢速 EMA 周期。 |
| `MacdSignalPeriod` | MACD 信号 EMA 周期。 |
| `TenkanPeriod` | Ichimoku Tenkan-sen 周期。 |
| `KijunPeriod` | Ichimoku Kijun-sen 周期。 |
| `SenkouSpanBPeriod` | Ichimoku Senkou Span B 周期。 |
| `TakeProfitPoints` | 止盈距离（点数），0 表示禁用。 |
| `StopLossPoints` | 止损距离（点数），0 表示禁用。 |
| `CandleType` | 所有指标使用的时间周期。 |

## 使用提示
- 仅在 K 线收盘后更新指标，遵循 EA 中 `shift=1` 的处理方式。
- 不同市场的动量刻度可能差异较大，切换品种时请调整 `MomentumThreshold`。
- 止损/止盈在策略内部管理，不会向交易所发送条件单。
- 如启用了图表，将展示价格 K 线、两条 LWMA、Ichimoku 云图以及成交记录。
