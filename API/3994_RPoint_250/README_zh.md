# RPoint 250 反转策略
[English](README.md) | [Русский](README_ru.md)

**RPoint 250 反转策略** 是 MetaTrader 4 专家顾问 `e_RPoint_250` 在 StockSharp 平台上的移植版本。原始脚本依赖自定义
指标 *RPoint* 来标记最近的摆动高点与低点。由于该指标在 StockSharp 中不可用，本实现改用内置的 `Highest` 与 `Lowest`
指标重建同样的逻辑。当新的极值取代旧的极值时，策略会立即反手，并重新应用与 MQL 版本一致的止损、止盈和拖尾机制。

## 交易流程

1. 订阅 `CandleType` 指定的 K 线序列（默认 5 分钟）。
2. 计算最近 `ReversePoint` 根 K 线的最高价与最低价，这两个数值即为模拟的 RPoint 水平。
3. 若出现新的最高价，关闭所有多头仓位并按 `OrderVolume` 开立空单。
4. 若出现新的最低价，关闭所有空头仓位并按 `OrderVolume` 开立多单。
5. 通过 `StartProtection` 设置保护性订单，`StopLossPoints` 与 `TakeProfitPoints` 以价格点数表示。
6. 当 `TrailingStopPoints` 大于零时启用拖尾：价格自入场以来移动的最大幅度回撤超过该阈值时，仓位将被平仓。
7. 记录最近一次进场所在 K 线的开盘时间，避免在同一根 K 线上重复下单，对应 MQL 中的 `TimeN` 逻辑。

策略始终只持有一侧仓位，在反向进场前会先行平仓。

## 参数说明

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `OrderVolume` | `decimal` | `0.1` | 每次市价单的手数，对应原版的 `Lots` 输入。 |
| `TakeProfitPoints` | `decimal` | `15` | 止盈距离（点数）。设为 `0` 可禁用固定止盈。 |
| `StopLossPoints` | `decimal` | `999` | 止损距离（点数）。设为 `0` 可关闭固定止损。 |
| `TrailingStopPoints` | `decimal` | `0` | 拖尾幅度（点数）。为零时不启用拖尾。 |
| `ReversePoint` | `int` | `250` | 搜索最近极值时回溯的 K 线数量。数值越大越平滑但反应更慢。 |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | 用于分析的 K 线周期，可根据 MetaTrader 图表调整。 |

## 实现细节

- 通过高阶 API `Bind` 将 `Highest` 与 `Lowest` 绑定到行情订阅，无需手动维护缓存队列。
- `StartProtection` 使用绝对价格单位还原原策略的止损与止盈距离，StockSharp 会在持仓变化后自动挂单。
- 拖尾采用收盘 K 线评估：当价格从入场后的最佳水平回撤超过阈值时，以市价单离场。
- `_executedHighLevel` 与 `_executedLowLevel` 保存最近一次触发的极值，防止重复下单，相当于 MQL 代码中的
  `Reverse_High` / `Reverse_Low` 变量。
- `_lastSignalTime` 复制了 `TimeN` 的节流机制，保证每根 K 线最多只会触发一次进场。

## 使用建议

1. 将策略加载到支持目标品种与所选 K 线周期的组合上。
2. 根据账户规模与风控规则调整 `OrderVolume`。
3. 结合标的波动性调节 `ReversePoint`，以取得信号频率与稳定性之间的平衡。
4. 确认 `StopLossPoints`、`TakeProfitPoints` 与 `TrailingStopPoints` 与品种的最小跳动价位 (`PriceStep`) 相兼容。
5. 在 StockSharp Designer 或 Backtester 中回测，确认行为与原专家顾问一致后再用于真实资金。
6. 关注日志输出，便于核对策略的反手时机与风险控制动作。

由于 RPoint 指标采用近似实现，在不同数据源或报价舍入规则下可能与 MetaTrader 出现细微差异。正式使用前务必在自有
行情环境中复核策略表现。
