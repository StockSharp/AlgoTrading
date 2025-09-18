# X MAN 策略

## 概述

X MAN 策略将 MetaTrader 专家顾问 `X_MAN.mq4` 的核心逻辑迁移到 StockSharp 的高级 API。该系统通过一对线性加权移动平均线（LWMA）识别趋势突破，并结合高周期动量与月线 MACD 过滤信号，旨在在动量和趋势一致时参与行情延续。

## 交易逻辑

1. **趋势过滤**：在主要周期上，快速 LWMA 必须高于（做多）或低于（做空）慢速 LWMA，且差值至少为 `DistancePoints`。
2. **动量确认**：订阅更高周期的 K 线并计算动量指标。最近三次动量相对 100 的绝对偏离值中，至少有一次需要超过多头或空头阈值，方向才被允许。
3. **MACD 过滤**：在月线级别计算标准 MACD（12, 26, 9）。做多时要求 MACD 线高于信号线，做空时要求 MACD 线低于信号线。
4. **订单执行**：当所有过滤器一致时，以市价单开仓。如果出现反向信号且当前头寸方向相反或为空，则允许翻仓。

## 参数

| 参数 | 说明 |
|------|------|
| `CandleType` | 计算 LWMA 的主周期。 |
| `HigherCandleType` | 用于动量过滤的高周期。 |
| `MacdCandleType` | MACD 使用的周期（默认月线）。 |
| `FastMaPeriod` | 快速 LWMA 的周期。 |
| `SlowMaPeriod` | 慢速 LWMA 的周期。 |
| `MomentumPeriod` | 动量指标的回溯长度。 |
| `MomentumBuyThreshold` | 判定多头动量所需的最小偏离。 |
| `MomentumSellThreshold` | 判定空头动量所需的最小偏离。 |
| `DistancePoints` | 两条 LWMA 之间的最小价格点差。 |
| `TakeProfitPoints` | 可选的止盈距离（点）。 |
| `StopLossPoints` | 可选的止损距离（点）。 |

所有参数均通过 `StrategyParam<T>` 暴露，可在 StockSharp Designer 中优化或在运行时配置。

## 风险管理

当 `TakeProfitPoints` 或 `StopLossPoints` 大于零时，策略会启用 StockSharp 的保护模块，并使用市价单退出。原始顾问中的保本和跟踪止损尚未移植。

## 与原始顾问的差异

- MetaTrader 版本包含权益止损、保本和复杂的资金管理。本移植专注于方向过滤与进场逻辑。
- 仓位大小由宿主环境决定，原始的递增手数算法未实现。
- 邮件、推送通知以及手动修改挂单的功能被省略。

这些调整保持策略简洁，充分利用 StockSharp 高级 API，同时保留原策略的主要思想。
