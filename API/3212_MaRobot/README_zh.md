# MaRobot 策略

## 概览
- 在可配置的日内周期上运行的均线交叉策略，并结合日线级别的 ADX 与 RSI 滤波。
- 通过 StockSharp 的高阶 `Bind` 机制同时计算快、慢 `SimpleMovingAverage`、`Lowest`/`Highest` 极值指标，以及日线的 `AverageDirectionalIndex` 与 `RelativeStrengthIndex`。
- 完整还原 MT4 版本的风险控制：百分比止盈、基于最近摆动点的止损，以及在收益达到阈值后启用的保本止损。

## 指标
- 主周期上的快、慢 `SimpleMovingAverage`。
- `Lowest` / `Highest`：追踪最近 `BackClose` 根 K 线的最低价/最高价，用于摆动止损。
- 日线 `AverageDirectionalIndex` 与 `RelativeStrengthIndex`：衡量趋势强度与动量。

## 参数
- `CandleType` – 主周期（默认 15 分钟 K 线）。
- `FastPeriod`、`SlowPeriod` – 快慢均线长度。
- `AdxThreshold` – 允许开仓的日线 ADX 上限。
- `RsiThreshold` – 多头入场的日线 RSI 阈值（空头使用 `100 - RsiThreshold`）。
- `TakeProfitRatio` – 相对入场价的止盈比例。
- `StopLossPoints` – 在达到 `ProtectThreshold` 后启用的保本止损距离（以合约最小变动单位计）。
- `ProtectThreshold` – 触发保本止损所需的最小浮盈比例。
- `BackClose` – 计算摆动高低点时回溯的已完成 K 线数量。
- `DailyAdxPeriod`、`DailyRsiPeriod` – 日线指标的周期长度。

## 交易逻辑
1. 仅在 K 线收盘后处理信号，以保持与 MT4 专家的行为一致。
2. 等待所有指标形成，并确保已获取日线 ADX/RSI 的最新数值。
3. **入场条件**：
   - 当日线 ADX 高于 `AdxThreshold` 时禁止开仓。
   - 多头需要快均线上穿慢均线且日线 RSI < `RsiThreshold`。
   - 空头需要快均线下穿慢均线且日线 RSI > `100 - RsiThreshold`。
4. 建仓后记录对应摆动极值（多头使用 `Lowest`，空头使用 `Highest`）作为手动止损线。
5. **持仓管理**：
   - 浮盈达到 `TakeProfitRatio` 时立即止盈。
   - 收盘价突破记录的摆动止损线时平仓。
   - 出现反向均线交叉时平仓。
   - 当盈利超过 `ProtectThreshold` 时，根据 `StopLossPoints` 计算保本价（按最小跳动单位四舍五入），若价格回落/反弹至该价位则平仓。
6. 仓位归零后重置所有内部状态。

## 说明
- C# 源码中的注释全部使用英文，符合仓库要求。
- 策略完全依赖 StockSharp 的高阶订阅，避免直接访问指标缓存。
- 按任务要求不提供 Python 版本。
