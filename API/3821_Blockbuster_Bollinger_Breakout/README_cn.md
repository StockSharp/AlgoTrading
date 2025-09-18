# Blockbuster Bollinger Breakout 策略
[English](README.md) | [Русский](README_ru.md)

Blockbuster Bollinger Breakout 策略来自 MetaTrader 4 的 “BLOCKBUSTER EA”。原版通过检测价格突破布林带并超出额外偏移量来寻找反转机会。本移植版保留了关键的信号、止盈和止损设置，同时利用 StockSharp 的高层 API 实现指标绑定、蜡烛订阅和仓位管理。

## 核心思路

1. 使用用户定义的周期和偏差构建布林带。
2. 当蜡烛的收盘价高于上轨（加上偏移量）或低于下轨（减去偏移量）时触发交易信号。
3. 收盘价高于上轨 + 偏移量 → 开空；收盘价低于下轨 – 偏移量 → 开多。
4. 按照点数计算的止盈和止损管理仓位，与 MQL 原版一致。

所有距离和阈值都以品种的“点”为单位，并自动乘以 `PriceStep`。例如 `3` 表示 3 个最小变动价位。

## 细节说明

- **指标设置**
  - 使用布林带指标，输入为蜡烛收盘价（原 EA 采用开盘价，本实现选择收盘价以适配 StockSharp 的蜡烛订阅流程）。
  - 参数：`BollingerPeriod`、`BollingerDeviation`。
  - 额外参数 `DistancePoints` 控制偏移距离。

- **入场条件**
  - **做多**：`Close < LowerBand - Distance` 且当前净头寸 ≤ 0。
  - **做空**：`Close > UpperBand + Distance` 且当前净头寸 ≥ 0。
  - 当方向反转时，订单数量为 `TradeVolume + |Position|`，确保一次只持有单方向仓位。

- **出场条件**
  - 在每根已完成蜡烛上计算未实现盈亏（点数）。
  - **止盈**：收益 ≥ `ProfitTargetPoints`。
  - **止损**：亏损 ≥ `LossLimitPoints`。
  - 设置为 0 即可关闭对应功能。止盈判定优先于止损，和原程序相同。

- **状态管理**
  - 信号触发时记录进场价格，用于后续盈利计算。
  - 仓位平仓后自动重置内部状态。

## 参数表

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `BollingerPeriod` | 20 | 布林带计算周期。 |
| `BollingerDeviation` | 2.0 | 标准差倍数。 |
| `DistancePoints` | 3 | 超出布林带的额外距离（点）。 |
| `ProfitTargetPoints` | 3 | 止盈阈值（点）。0 表示禁用。 |
| `LossLimitPoints` | 20 | 止损阈值（点）。0 表示禁用。 |
| `TradeVolume` | 1 | 下单数量，对应 MT4 的 Lots。 |
| `CandleType` | 1 分钟 | 计算所用蜡烛类型。 |

## 使用建议

- 适用于拥有明确 `PriceStep` 的产品，如外汇、指数 CFD、流动性好的期货。
  
- 由于指标改用收盘价，建议在目标周期上重新回测，确认与 MT4 的表现一致。
- 如果界面支持图表，会显示蜡烛、布林带以及策略成交，方便观察。
- 策略只在完整蜡烛上做判断，保证回测与实时的一致性。

## 标签

- 策略类型：反转突破
- 方向：双向
- 指标：Bollinger Bands
- 止损：支持（可配置）
- 周期：短周期（默认 1 分钟）
- 复杂度：简单

