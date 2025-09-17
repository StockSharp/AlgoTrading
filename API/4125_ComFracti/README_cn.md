# ComFracti 策略

## 概览

ComFracti 策略移植自 MT4 的 "ComFracti" 智能交易程序。策略核心是将不同时间框架上的分形信号与 RSI 和随机振荡器过滤器相结合，并提供 EMA 斜率、抛物线 SAR、通道突破以及感知器等可选过滤器，以控制趋势方向。C# 版本基于 StockSharp 高级 API，只在完成的 K 线上评估信号，并且同一时间仅持有一笔仓位。

## 交易逻辑

- **主信号**
  - 当当前时间框架与高一级时间框架同时出现看涨分形时触发多头确认。
  - 当两个时间框架同时出现看跌分形时触发空头确认。
  - 启用 RSI 过滤器时，需要高时间框架上的 3 周期 RSI 低于 `50 - RsiLevelBuy`（做多）或高于 `50 + RsiLevelSell`（做空）。
  - 启用随机振荡器过滤器时，%K（默认周期 5，%D 平滑 3/3）需低于 `50 - StochasticLevelBuy`（做多）或高于 `50 + StochasticLevelSell`（做空）。
- **可选过滤器**
  - **EMA 斜率**：过滤时间框架上的 EMA 必须上行（多头）或下行（空头）。
  - **抛物线 SAR**：当前 K 线开盘价需位于 SAR 值之上（多头）或之下（空头）。
  - **通道突破**：比较上一根 K 线与自适应的 Donchian 通道，上一根最低价需高于下轨（多头），上一根最高价需低于上轨（空头）。
  - **感知器过滤器**：最近高低点差的加权和需大于零（多头）或小于零（空头）。
- **仓位管理**
  - 策略仅保持单一方向仓位；若出现相反方向的交易机会，将先平掉原有仓位再入场。
  - 止损与止盈距离以品种最小波动点数设置。
  - 可选的追踪止损在价格达到缓冲区后沿利润方向移动（`ProfitTrailing` 为真时生效）。
  - 启用 `CloseOnOppositeSignal` 后，当出现反向主信号时提前离场。

## 风险管理

- 基础下单数量由 `BaseVolume` 控制（默认 0.1 手），启用 `AccountMicro` 时自动除以 10。
- 启用 `UseMoneyManagement` 后，策略会根据 `RiskPercent` 所定义的风险百分比、当前止损距离以及合约步值估算仓位规模，并保证结果不低于 `MinimumVolume`。

## 参数说明

| 名称 | 说明 |
| --- | --- |
| `TakeProfitPoints`, `StopLossPoints` | 止盈与止损距离（点）。 |
| `UseTrailingStop`, `TrailingStopPoints`, `ProfitTrailing` | 追踪止损设置（距离以及是否需要浮盈）。 |
| `BaseVolume`, `UseMoneyManagement`, `RiskPercent`, `AccountMicro`, `MinimumVolume` | 仓位管理参数。 |
| `UseFractals`, `FractalShift*` | 是否启用分形确认及分形回溯位置。 |
| `UseRsi`, `RsiLevelBuy`, `RsiLevelSell`, `RsiType` | RSI 过滤器阈值与时间框架。 |
| `UseStochastic`, `StochasticPeriod*`, `StochasticLevel*` | 随机振荡器周期与阈值。 |
| `UseMaFilter`, `MaPeriod` | EMA 过滤器配置。 |
| `UsePsarFilter`, `PsarStep` | 抛物线 SAR 过滤器配置。 |
| `UseChannelFilter`, `ChannelLookback`, `ChannelK` | 通道突破过滤器配置。 |
| `UsePerceptronFilter`, `PerceptronV1`–`PerceptronV4` | 感知器权重（0–100，围绕 50 调整）。 |
| `CandleType`, `HigherFractalType`, `FilterType` | 策略使用的主、副时间框架。 |

## 备注

- 策略只处理已收盘的 K 线，因此与原始基于 tick 的 EA 相比可能存在细微差异。
- 分形跟踪器复刻了 MT4 的五根 K 线分形逻辑，并支持自定义回溯位移（对应原版的 `sh1/sh2` 参数）。
- 资金管理依赖 StockSharp 提供的账户估值；若无法获取估值，则退回到固定基础手数。
