# Tunnel Method 策略

[English](README.md) | [Русский](README_ru.md)

Tunnel Method 策略是 MetaTrader 5 平台上同名专家顾问的 StockSharp 移植版本。策略通过三条具有正向偏移的简单移动平均线（SMA）构建价格“隧道”。当最快的均线突破隧道并满足最小间距条件时，触发入场信号。该实现完整保留了原始 MQL 逻辑：点值制的止损、止盈、按步进更新的追踪止损以及评估信号之间的最小等待时间。

## 策略逻辑

- **指标结构**：三条 SMA 使用同一品种和同一时间框架。
  - *第一条 SMA*（慢线）：长周期、无偏移，用于界定多头隧道的下沿和空头隧道的上沿。
  - *第二条 SMA*（中线）：中等周期、正偏移，主要用于空头信号的前置屏障。
  - *第三条 SMA*（快线）：短周期、最大偏移，当其突破隧道时产生交易信号。
- **最小间距**：快线必须至少与慢线/中线保持 `IndentPips`（换算为价格单位）的距离，以过滤震荡行情。多头信号要求上一根K线上快线低于慢线减半个间距、当前K线上快线高于慢线加半个间距；空头信号与之相反。
- **评估节奏**：每次评估后需等待 `PauseSeconds` 秒才会再次检查信号，模拟原版 EA 对 OnTick 调用频率的限制。
- **持仓限制**：策略同一时间只允许一笔仓位，若已有持仓则忽略新的入场条件。

## 风险控制

- **止损**：`StopLossPips` 以点数设置，分别作用于多头和空头的入场价下方/上方。设为 0 可关闭。
- **止盈**：`TakeProfitPips` 以点数设置固定目标。
- **追踪止损**：当 `TrailingStopPips` 和 `TrailingStepPips` 都大于 0 时启用。价格向有利方向移动 `TrailingStopPips + TrailingStepPips` 点后，止损移动到距离当前收盘价 `TrailingStopPips` 的位置，且只有在价格进一步至少前进一个步长时才会再次更新。
- **离场方式**：当价格触发止损、止盈或追踪止损价格时，策略使用市价单平仓，以贴合原策略中由经纪商执行保护单的行为。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TradeVolume` | 1 | 每次入场的交易量。 |
| `StopLossPips` | 50 | 止损点数，0 表示禁用。 |
| `TakeProfitPips` | 50 | 止盈点数，0 表示禁用。 |
| `TrailingStopPips` | 5 | 追踪止损的基础距离，需要与 `TrailingStepPips` 搭配使用。 |
| `TrailingStepPips` | 5 | 追踪止损再次调整所需的最小新增盈利。 |
| `FirstMaPeriod` | 160 | 慢速 SMA 周期。 |
| `FirstMaShift` | 0 | 慢速 SMA 的正向偏移。 |
| `SecondMaPeriod` | 80 | 中速 SMA 周期。 |
| `SecondMaShift` | 1 | 中速 SMA 的正向偏移。 |
| `ThirdMaPeriod` | 20 | 快速 SMA 周期。 |
| `ThirdMaShift` | 2 | 快速 SMA 的正向偏移。 |
| `IndentPips` | 1 | 均线之间的最小允许间距。 |
| `PauseSeconds` | 45 | 每次信号评估之间的等待时间（秒）。 |
| `CandleType` | 5 分钟 K 线 | 用于计算指标的蜡烛序列。 |

所有以点数为单位的参数都会根据 `PriceStep` 和证券的小数位自动转换为价格距离，对三位或五位报价的外汇品种也进行了与原 EA 相同的调整。

## 实践建议

1. **检查品种精度**：确保 `Security` 的 `PriceStep` 与 `Decimals` 已正确配置，否则点数转换为价格会产生误差。
2. **匹配时间框架**：默认使用 5 分钟 K 线，可根据需要改成与原 MT5 模型一致的时间框架（如 M1）。
3. **仓位管理**：`TradeVolume` 决定初始仓位规模，平仓时使用相同的市价交易量，因此整体仓位保持一致。
4. **追踪止损约束**：如果设置了 `TrailingStopPips > 0` 而 `TrailingStepPips == 0`，构造函数会抛出异常，这与原始实现的参数检查一致。
5. **参数优化**：所有参数都通过 `Param` 定义，方便在 StockSharp Designer 中优化或暴露到界面上进行调节。

## 文件结构

- `CS/TunnelMethodStrategy.cs`：核心策略代码。
- `README.md`：英文文档。
- `README_ru.md`：俄文文档。
- `README_cn.md`：中文文档（本文件）。

根据任务要求，暂未提供 Python 版本，当前仅包含 C# 实现。
