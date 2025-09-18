# Macd Pattern Trader All v0.01
[English](README.md) | [Русский](README_ru.md)

该策略复现 MetaTrader 专家顾问 “MacdPatternTraderAll v0.01”。它在同一根K线数据上同时运行六套独立的 MACD 入场模式，通过自适应的止损与止盈控制风险，按照原始 EA 的方式分批止盈，并可在亏损后启用缓慢的马丁加仓规则。

## 主要特点

- **六种 MACD 信号**——每个模式 (`Pattern1` … `Pattern6`) 都拥有自己的快/慢 EMA 周期和阈值，并可单独启用或禁用。
- **动态风险控制**——止损根据最近的高点/低点加上可配置的点差偏移量计算，止盈通过连续的区段扫描来完全复现 MQL 中的 `iLowest` / `iHighest` 循环。
- **时间过滤**——当 `UseTimeFilter` 为真时，仅在 `StartTime` 与 `StopTime` 定义的时段内交易。
- **分批止盈**——盈利仓位会分两步减仓：先在 `ema2` 确认的利润目标处减掉三分之一，再在 `(sma3 + ema4) / 2` 水平减掉一半余量。
- **缓慢马丁策略**——`UseMartingale` 打开时，如果一个交易循环以亏损结束，下一次下单的基础手数会翻倍；只要循环盈利则恢复到初始手数。

## 各模式入场逻辑

1. **Pattern 1 (`Pattern1`)**：当 MACD 主线冲破 `Pattern1MaxThreshold` 后回落形成更低的峰值时做空；跌破 `Pattern1MinThreshold` 后抬高低点时做多。
2. **Pattern 2 (`Pattern2`)**：监控零轴附近的摆动。正向摆动在 `Pattern2MinThreshold` 附近衰竭时做空；负向摆动在 `Pattern2MaxThreshold` 周围衰竭时做多，并保留对 `valueMin2` / `valueCurr2` 的绝对值比较。
3. **Pattern 3 (`Pattern3`)**：跟踪最多三个连续的 MACD 顶/底，形成 “三重钩” 形态，只有在所有阈值 (`Pattern3MaxThreshold`, `Pattern3MaxLowThreshold`, `Pattern3MinThreshold`, `Pattern3MinHighThreshold`) 同时满足时才允许入场。
4. **Pattern 4 (`Pattern4`)**：当 MACD 突破 `Pattern4MaxThreshold` / `Pattern4MinThreshold` 且未能再创新高（低）时触发，保留 `Pattern4AdditionalBars` 计数器以兼容原版 EA。
5. **Pattern 5 (`Pattern5`)**：实现 EA 中的“中性区突破”。价格先从极值回到中性区 (`Pattern5MinNeutralThreshold` 或 `Pattern5MaxNeutralThreshold`)，随后再次反向失败即入场。
6. **Pattern 6 (`Pattern6`)**：统计连续位于阈值外的 K 线数量。若在超买/超卖区域停留超过 `Pattern6TriggerBars` 并重新回到阈值以内，而且没有被 `Pattern6MaxBars` 禁止，则开仓。

所有模式均调用 `TryOpenLong` / `TryOpenShort`，确保在发单前已经算好止损与止盈。

## 风险与仓位管理

- **止损**：`CalculateStopPrice` 读取最近 `stopBars` 根已完成的K线（不含当前）并加上 `offset`，对三/五位小数的品种自动做精度修正。
- **止盈**：`CalculateTakeProfit` 按 `takeBars` 为步长遍历历史区段，只要后续区段出现更远的极值就继续迭代，直到不再刷新，完全复制原始 MQL 逻辑。
- **分批减仓**：`ManageActivePositions` 在利润超过 `ProfitThreshold` 且 `ema2` 同向时卖出三分之一仓位，再在 `(sma3 + ema4) / 2` 水平卖出剩余仓位的一半。
- **强制离场**：`CheckRiskManagement` 监控保存的止损与止盈，一旦触发立即以市价平仓。
- **马丁调整**：`OnOwnTradeReceived` 统计当前平仓周期的盈亏，`AdjustVolumeOnFlat` 在仓位归零后根据结果重置或加倍下一笔交易量。

## 参数说明

所有设置均以 `StrategyParam<T>` 暴露，可在 StockSharp Designer 中优化。

- **通用设置**：`CandleType`, `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`。
- **模式 1–6**：与 EA 中外部变量一致的止损/止盈窗口、偏移量、MACD 周期以及阈值。
- **仓位管理**：`EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4` 用于分批平仓判断。

默认值全部对应 `MacdPatternTraderAll v0.01` 的输入参数。

## 使用建议

- 需要为交易品种配置正确的 `PriceStep` 和 `Decimals`，以便精确计算价格偏移。
- 通过 `CandleType` 提供蜡烛图数据（例如 `TimeSpan.FromMinutes(5).TimeFrame()`）。
- 当多个模式同时触发时，仅会建立一笔新的净仓位，因为每次入场都会重新计算总下单量并清理反向止损/止盈。
- 分批平仓针对的是净仓位，因此即使不同模式给出同方向信号，减仓逻辑依旧生效。

