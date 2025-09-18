# 通用跟踪管理策略

## 概述

**Universal Trailing Manager Strategy** 是 MetaTrader 指标 “Universal 1.64 (barabashkakvn 修改版)” 的 C# 版本。
它专注于自动化交易管理：按时间窗口执行操作、维护买卖止损/限价挂单、跟踪市价单及挂单、快速锁定浮盈、
并在账户权益达到指定百分比时发出提示，非常适合手动交易或半自动系统的风控模块。

策略不依赖技术指标，只需要稳定的 K 线数据即可运行，因此可以灵活地集成到任何基于 StockSharp 的策略框架中。

## 主要特性

- **定时操作**：在指定的终端时间（小时/分钟）自动下达市价单或挂单。
- **挂单管理**：同时维护最多四种挂单（买限、卖限、买止、卖止），可独立设置价格偏移、止盈止损及拖尾参数，
  并在行情有利移动时自动重新挂单。
- **持仓保护**：对当前净头寸应用止盈止损与拖尾逻辑，可选择在浮盈达到拖尾距离后才启动移动保护。
- **剥头皮退出**：当价格较平均持仓价移动指定点数时，立即平掉现有仓位。
- **账户提醒**：监控组合权益，当权益相对初始值上涨或下跌到设定百分比时在日志中提示。
- **仓位限制**：支持“等待仓位清零”模式，并可为多空方向分别设置最大持仓次数。

## 参数说明

| 分组 | 参数 | 说明 |
|------|------|------|
| 通用 | `TradeVolume` | 下单手数，适用于市价单与挂单。 |
| 通用 | `WaitClose` | 为 `true` 时，仅当当前方向仓位数量小于 `MaxMarketPositions` 才允许开新仓或挂单。 |
| 市价单 | `MaxMarketPositions` | 在启用 `WaitClose` 时，每个方向允许的最大仓位数。 |
| 市价单 | `MarketTakeProfitPoints` | 市价单止盈距离（点）。0 表示不设止盈。 |
| 市价单 | `MarketStopLossPoints` | 市价单止损距离（点）。0 表示不设止损。 |
| 市价单 | `MarketTrailingStopPoints` | 市价单拖尾距离（点）。0 表示关闭拖尾。 |
| 市价单 | `MarketTrailingStepPoints` | 调整拖尾前必须达到的最小改进幅度（点）。 |
| 市价单 | `WaitForProfit` | 启用后，仅当浮盈超过 `MarketTrailingStopPoints` 时才移动拖尾。 |
| 市价单 | `ScalpProfitPoints` | 当价格相对持仓均价达到该点数时立即平仓。0 表示关闭。 |
| 挂单 | `AllowBuyLimit` / `AllowSellLimit` / `AllowBuyStop` / `AllowSellStop` | 各类挂单的主开关。 |
| 挂单 | `LimitOrderOffsetPoints` / `StopOrderOffsetPoints` | 相对当前收盘价的挂单偏移距离，需大于交易所的最小止损距离。 |
| 挂单 | `LimitOrderTakeProfitPoints` / `StopOrderTakeProfitPoints` | 挂单成交后附带的止盈距离（点）。 |
| 挂单 | `LimitOrderStopLossPoints` / `StopOrderStopLossPoints` | 挂单成交后附带的止损距离（点）。 |
| 挂单 | `LimitOrderTrailingStopPoints` / `StopOrderTrailingStopPoints` | 挂单拖尾距离，0 表示不拖尾。 |
| 挂单 | `LimitOrderTrailingStepPoints` / `StopOrderTrailingStepPoints` | 每次拖尾调整所需的最小改进幅度。 |
| 时间 | `UseTime` | 是否启用定时模块。 |
| 时间 | `TimeHour`, `TimeMinute` | 触发定时动作的终端时间。 |
| 时间 | `TimeBuy`, `TimeSell` | 在指定时间直接开多/开空。 |
| 时间 | `TimeBuyLimit`, `TimeSellLimit`, `TimeBuyStop`, `TimeSellStop` | 在指定时间强制挂出相应挂单（忽略主开关）。 |
| 全局 | `UseGlobalLevels` | 是否监控账户整体盈亏。 |
| 全局 | `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | 账户权益上升/下降达到百分比时发出提示。 |
| 数据 | `CandleType` | 用于驱动策略的 K 线类型（默认 1 分钟）。 |

## 执行流程

1. **接收 K 线**：每根完成的 K 线都会触发挂单/止损引用更新及定时信号刷新。
2. **定时检查**：若当前 K 线收盘时间与设置的小时/分钟一致，策略立即执行对应的市价或挂单动作。
3. **挂单维护**：每种挂单只保留一张，当价格满足拖尾条件时撤销并按照最新价格重新挂出。
4. **持仓保护**：根据拖尾与止盈止损设置，动态维护用于保护当前持仓的止损止盈单，并确保数量等于净头寸。 
5. **剥头皮退出**：若 `ScalpProfitPoints` 大于 0，则一旦达到目标点差立即平仓。
6. **账户提醒**：每轮循环检查组合权益，首次达到设置阈值时在日志中提示。

## 使用建议

- 策略依赖 K 线事件而非逐笔数据，建议使用较小周期（例如 1 分钟）以提高跟踪精度。
- 策略以净头寸 (`Position`) 进行计算，若需要反手，会自动在下单量中加入已有反向仓位以实现平仓再开仓。
- 所有点数均以 `Security.PriceStep` 为基准，确保在交易连接中正确配置合约的最小价格变动单位。
- 全局监控功能仅在日志中提示，不会自动平仓，与原版 EA 行为保持一致。
- 在启用 `WaitClose` 且每次开仓量一致的前提下，最大仓位控制才能准确生效。

## 日志

策略使用 `LogInfo` 输出关键动作（挂单、撤单、拖尾调整、账户提醒等）。调试或优化参数时，请关注日志以了解其决策流程。

