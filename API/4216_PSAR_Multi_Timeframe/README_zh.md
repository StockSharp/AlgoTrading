# PSAR 多周期策略

## 概述
本策略移植自 MetaTrader 专家顾问 **EA_PSar_002B**。它在 M15、M30 与 H1 三个周期上计算 Parabolic SAR，同时使用 1 分钟数据管理仓位。系统一次只允许持有一个净仓位；前一笔仓位完全平仓后才会触发新的信号。原始脚本是针对 EURUSD 的 M1 图表开发的，移植版本保持这一假设。

## 交易规则
1. **SAR 收敛过滤器**：M15、M30、H1 的最新 SAR 值之间的距离必须不超过 19 个最小价格跳动，只有当三条曲线充分靠拢时才允许进场。
2. **多头条件**（满足任意一组即可）：
   - 三个周期的 SAR 都在各自当前最低价之下，同时上一根 H1 SAR 高于上一根 H1 最高价，而当前 H1 SAR 跌破当前 H1 最低价。
   - M15 与 H1 的 SAR 位于当前最低价以下，上一根 M30 SAR 高于上一根 M30 最高价，而当前 M30 SAR 跌破当前 M30 最低价。
   - M30 与 H1 的 SAR 位于当前最低价以下，上一根 M15 SAR 高于上一根 M15 最高价，而当前 M15 SAR 跌破当前 M15 最低价。
3. **空头条件** 为上述规则的镜像：把“低/高”互换即可。
4. **止盈止损**：目标和保护价以最小跳动数表示，默认分别为 999 与 399 点，对应 MQL 版本在 4/5 位报价下的配置。
5. **动态离场**：持仓期间监控 M30 的 SAR。
   - 多头：如果上一根 SAR 低于上一根 M1 最低价，而当前 SAR 突破当前 M1 最高价，则立即平仓。
   - 空头：如果上一根 SAR 高于上一根 M1 最高价，而当前 SAR 跌破当前 M1 最低价，则立即平仓。
   - 当当前 M30 SAR 穿越开仓价时，止损会被上调/下调至该 SAR 水平，实现跟踪保护。

## 资金管理
`UseMoneyManagement` 与原始 EA 的开关保持一致。关闭时按照 `FixedVolume` 下单；开启后根据 `PercentMoneyManagement` 指定的资金百分比计算合成手数（百分比 × 可用资金 ÷ 100000），并按 `Security.VolumeStep` 对齐，同时受 `VolumeMin`/`VolumeMax` 限制。

## 参数
- `BaseCandleType`：用于撮合与风控的基础周期（默认 M1）。
- `FastSarCandleType`、`MediumSarCandleType`、`SlowSarCandleType`：三个 SAR 的周期，默认分别为 15/30/60 分钟。
- `EnableParabolicFilter`：对应 MQL 的 `sar2` 标志，关闭后策略保持空闲。
- `TakeProfitPoints`、`StopLossPoints`：止盈止损的点数。策略根据 `Security.PriceStep` 和 `Security.Decimals` 自动推导“pip”大小，因此适用于 3 位与 5 位报价。
- `UseMoneyManagement`、`PercentMoneyManagement`、`FixedVolume`：体积控制相关设置。

## 移植说明
- 仅使用 StockSharp 的高层 API，通过 `SubscribeCandles().Bind(...)` 订阅多个周期并获取指标输出，无需手工缓存。
- 防护逻辑通过市价平仓实现，与原脚本调用 `OrderClose` 的行为一致。
- MQL 中根据报价位数调整点值的逻辑，被自动的步长推导 (`PriceStep` × 10) 取代。
- 原 EA 会在非 EURUSD 或非 M1 图表上弹出提示，移植版不再强制阻止，但在文档中明确推荐。

## 使用建议
1. 在 EURUSD 上加载策略，并确保提供 M1 数据流；若需要实验，可修改各 SAR 的周期。
2. 交易所需提供 `PriceStep` 与 `Decimals` 元数据，否则止盈止损的换算将退化为 1 个单位。
3. 一旦需要停止交易，可将 `EnableParabolicFilter` 设为 `false`；保持开启才能执行完整逻辑。
