# EMA WMA 逆势策略
[English](README.md) | [Русский](README_ru.md)

该策略以蜡烛的开盘价计算指数移动平均线（EMA）与加权移动平均线（WMA），在两者发生反向穿越时进行逆势交易。EMA 从上向下穿越 WMA 时买入，期待价格回归；EMA 从下向上穿越 WMA 时做空。仓位规模依据风险百分比和止损距离动态调整，同时提供固定止损、止盈和跟踪止损以控制风险。

## 细节

- **入场条件**：
  - 多头：EMA(Open) 从上向下穿越 WMA(Open)
  - 空头：EMA(Open) 从下向上穿越 WMA(Open)
- **方向**：双向
- **离场条件**：
  - 固定止损（按价格步长）
  - 固定止盈（按价格步长）
  - 当价格至少上涨 `TrailingStopPoints + TrailingStepPoints` 后，跟踪止损上移或下移
  - 反向穿越会关闭当前仓位并开出相反方向
- **止损**：止损、止盈和跟踪止损
- **默认值**：
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossPoints` = 50m
  - `TakeProfitPoints` = 50m
  - `TrailingStopPoints` = 50m
  - `TrailingStepPoints` = 10m
  - `RiskPercent` = 10m
  - `BaseVolume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选标签**：
  - 类别：移动平均线，逆势
  - 方向：多头与空头
  - 指标：EMA (open)、WMA (open)
  - 止损：有（固定止损 + 跟踪止损）
  - 复杂度：中等
  - 周期：日内（默认 1 分钟）
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

## 参数

| 参数 | 说明 |
| --- | --- |
| `EmaPeriod`, `WmaPeriod` | 以开盘价计算的 EMA 与 WMA 周期。 |
| `StopLossPoints`, `TakeProfitPoints` | 止损与止盈距离（价格步长单位）。 |
| `TrailingStopPoints` | 跟踪止损与当前价格之间的距离。 |
| `TrailingStepPoints` | 跟踪止损调整前所需的额外盈利距离，启用跟踪时必须为正。 |
| `RiskPercent` | 每笔交易冒风险的账户百分比，头寸规模按 `RiskPercent / (StopLossPoints * PriceStep)` 计算。 |
| `BaseVolume` | 无法计算风险仓位时使用的最小下单量。 |
| `CandleType` | 参与计算的蜡烛类型（默认 1 分钟）。 |

## 说明

- 两条均线都使用开盘价，与原始 MetaTrader 专家顾问保持一致。
- 跟踪止损仅在价格向有利方向移动至少 `TrailingStopPoints + TrailingStepPoints` 后才开始移动。
- 如果设置了 `TrailingStopPoints` 而 `TrailingStepPoints` 小于等于 0，策略会立即停止以避免不一致的跟踪行为。
- 当账户权益、价格步长或止损距离不可用时，会回退到 `BaseVolume` 作为下单数量。
