# Larry Connors RSI-2 策略
[English](README.md) | [Русский](README_ru.md)

该策略是经典 Larry Connors RSI-2 系统的 StockSharp 移植版本。它在小时线级别结合 2 周期 RSI、5 周期和 200 周期简单移动平均线，通过均值回归思路捕捉短期反弹，同时顺应长期趋势。可选的止损、止盈参数以点（pip）为单位，实现与 MetaTrader 原版一致的资金管理。

## 策略概览

- **类型**：带趋势过滤的均值回归。
- **市场**：面向外汇货币对，默认使用 H1 K 线。
- **方向**：支持做多与做空，但需符合慢速均线方向。
- **指标组合**：SMA(5) 负责离场，SMA(200) 负责趋势过滤，RSI(2) 负责触发信号。

## 交易规则

### 做多条件
- RSI 数值低于 `RSI Long Entry` 阈值（默认 6）。
- 蜡烛线收盘价位于 `Slow SMA` 上方。
- 当前没有持仓。

### 做空条件
- RSI 数值高于 `RSI Short Entry` 阈值（默认 95）。
- 收盘价低于 `Slow SMA`。
- 当前为空仓。

### 平仓逻辑
- **多头**：当收盘价上穿 `Fast SMA`（默认 5）时平仓，可选的止损/止盈触发时同样平仓。
- **空头**：当收盘价下穿 `Fast SMA` 时平仓，止损/止盈同样适用。

### 风险控制
- `Use Stop Loss` 控制是否启用按点数计算的固定止损。
- `Use Take Profit` 控制是否启用对称的固定止盈。
- 点数通过品种的 `PriceStep` 与小数位数自动换算成价格，兼容 4 位和 5 位报价模型。

## 默认参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Trade Volume` | 1 | 每次进场的下单量。 |
| `Fast SMA Period` | 5 | 快速均线长度。 |
| `Slow SMA Period` | 200 | 趋势过滤均线长度。 |
| `RSI Period` | 2 | RSI 观察周期。 |
| `RSI Long Entry` | 6 | 做多触发阈值。 |
| `RSI Short Entry` | 95 | 做空触发阈值。 |
| `Use Stop Loss` | true | 是否启用止损。 |
| `Stop Loss (pips)` | 30 | 止损距离（点）。 |
| `Use Take Profit` | true | 是否启用止盈。 |
| `Take Profit (pips)` | 60 | 止盈距离（点）。 |
| `Candle Type` | 1 小时 | 使用的 K 线类型。 |

上述关键参数全部支持 `.SetCanOptimize(true)`，便于在 Designer 或 Tester 中批量优化。

## 实施细节

- 信号在蜡烛线收盘后计算，确保与 MetaTrader 版本保持一致。
- 策略内部跟踪入场价，并在达到止损或止盈时使用市价单全部平仓。
- 每次启动都会重置点值和入场缓存，保证回测可重复。
- 建议使用高质量的外汇历史数据，以评估策略在不同货币对上的表现。

## 使用建议

1. 连接能够提供 1 小时 K 线的外汇数据源。
2. 在 StockSharp Designer 中加载策略，或通过 API 直接运行。
3. 根据经纪商合约细则调整止损/止盈点数。
4. 如需迁移到其他品种，可优化 RSI 阈值与均线周期。

该实现完整复刻了 Larry Connors RSI-2 的核心思想，方便在 StockSharp 平台中与其他策略组件组合或比较。
