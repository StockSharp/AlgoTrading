# Laptrend_1 策略

## 概述
Laptrend_1 策略复刻了 MetaTrader 专家顾问 **Laptrend_1.mq4** 的交易逻辑。策略结合多周期 LabTrend 通道过滤、Fisher 变换动量确认以及基于 15 分钟 K 线的 ADX 趋势强度判断。只有当高周期（H1）和信号周期（M15）的 LabTrend 方向一致、Fisher 变换指示动量同向、ADX 显示趋势增强时才会开仓。当动量反转、LabTrend 改变方向或 ADX 与 DI 指标收敛显示横盘时，仓位将被平掉。

## 交易逻辑
- **核心数据**：15 分钟 K 线用于执行进出场，1 小时 K 线提供 LabTrend 趋势过滤。
- **LabTrend 通道**：通过回溯 `ChannelLength` 根 K 线构建类似 Donchian 的通道，并使用 `RiskFactor` 缩窄上下边界，重现 `LabTrend1_v2.1` 指标。当收盘价突破上轨时标记为多头趋势，跌破下轨则标记为空头趋势。只有当 M15 与 H1 趋势方向一致才允许入场。
- **Fisher 变换**：自定义的 `Fisher_Yur4ik` 指标在 15 分钟周期上跟踪动量。穿越零轴会翻转多空偏好，穿越 ±0.25 阈值会触发平仓信号。
- **ADX 过滤器**：15 分钟 ADX 必须走强，且主导的 DI 分量必须支持潜在交易方向。当 ADX、+DI、–DI 之间的差值低于 `Delta` 时，视为横盘，策略会复位动量标志并清空仓位。
- **仓位管理**：新信号会先平掉反向持仓，并按设定手数建仓。LabTrend 反向、Fisher 触发退出或市场进入横盘都会导致平仓。

## 风险控制
- **止损 / 止盈**：以品种点数（MetaTrader “点”）设置，通过比较蜡烛最高/最低价模拟原始 EA 的保护单。
- **移动止损**：当价格向有利方向推进后，`TrailingStopPoints` 会按照设定距离跟随收盘价，一旦价格回踩到移动止损立即市价离场。
- **手数**：所有订单使用固定的 `Volume` 参数（手）。

## 参数
- `Volume` – 下单手数，默认 1。
- `AdxPeriod` – ADX 平滑周期，默认 14。
- `FisherLength` – Fisher 变换窗口，默认 10。
- `ChannelLength` – LabTrend 通道回溯长度，默认 9。
- `RiskFactor` – LabTrend 缩窄系数（原指标建议 1..10），默认 3。
- `Delta` – ADX 与 DI 差值阈值，默认 7。
- `StopLossPoints` – 止损点数，默认 100。
- `TakeProfitPoints` – 止盈点数，默认 40。
- `TrailingStopPoints` – 移动止损点数，默认 100。
- `SignalCandleType` – 信号周期的 K 线类型（默认 15 分钟）。
- `TrendCandleType` – 趋势过滤所用 K 线类型（默认 1 小时）。

## 说明
- 原 MQL 程序在每个 tick 上运行，本移植版本在收盘后的 15 分钟 K 线处理信号，使计算更可重复，同时保留指标特性。
- 止损、止盈与移动止损通过判断蜡烛高低点触发市场单，等效于 MetaTrader 中的保护订单，而无需维护真实的挂单。
- 启动策略前请确保数据源同时提供 15 分钟与 1 小时两组 K 线数据。
