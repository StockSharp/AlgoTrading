# MACD Fixed PSAR 策略

## 概述
该策略为 MetaTrader 智能交易系统 **EA_MACD_FixedPSAR** 的 C# 版本。策略通过 MACD 金叉/死叉配合 EMA 趋势过滤来捕捉趋势反转，同时提供固定距离追踪止损与类抛物线 SAR 追踪止损两种风险管理选项。所有阈值均以点数（pips）配置，并根据品种的最小跳动点自动转换为价格单位。

## 指标
- `MovingAverageConvergenceDivergenceSignal`（12, 26, 9）提供 MACD 线与信号线。
- `ExponentialMovingAverage`（默认 26）作为短期趋势过滤器。

## 交易逻辑
1. **入场**
   - **做多**：MACD 在零轴下方向上穿越信号线，MACD 绝对值超过“开仓阈值”，且 EMA 较上一根 K 线走高。
   - **做空**：MACD 在零轴上方向下穿越信号线，MACD 绝对值超过“开仓阈值”，且 EMA 较上一根 K 线走低。
2. **离场**
   - MACD 在相反方向的回调超过“平仓阈值”。
   - 以点数定义的止盈与止损触发。
   - 可选的追踪止损：
     - **Fixed**：保持与最新收盘价的固定距离。
     - **Fixed PSAR**：模拟原始 MQL 版本使用的逐步抛物线 SAR 调整方法。

## 参数
| 名称 | 说明 |
| ---- | ---- |
| `Volume` | 订单使用的交易量。 |
| `TakeProfitPips` | 止盈距离（点）。 |
| `StopLossPips` | 止损距离（点）。 |
| `TrailMode` | 追踪止损模式（`None`、`Fixed`、`FixedPsar`）。 |
| `TrailingStopPips` | 固定追踪模式的距离。 |
| `PsarStep` | PSAR 模式的初始加速因子。 |
| `PsarMaximum` | PSAR 模式的最大加速因子。 |
| `MacdOpenLevelPips` | 触发开仓所需的 MACD 最小幅度（点）。 |
| `MacdCloseLevelPips` | 触发平仓所需的 MACD 最小幅度（点）。 |
| `TrendPeriod` | EMA 趋势过滤器的周期。 |
| `CandleType` | 用于计算指标的 K 线类型。 |

## 注意事项
- 点数会根据合约的 `PriceStep` 自动转换，并包含三/五位小数品种的特殊调整，以贴合 MetaTrader 的处理方式。
- 追踪止损只在完整收盘的 K 线上更新，以避免噪音导致的提前离场。
- 若图表区域可用，策略会绘制行情 K 线、两个指标以及交易标记。
