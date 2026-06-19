# Graal EMA Momentum 策略
[English](README.md) | [Русский](README_ru.md)

本策略将 MetaTrader 4 智能交易系统 **0Graal-CROSSmuvingi** 转换到 StockSharp。它利用两条指数移动平均线：快速 EMA 基于收盘价，慢速 EMA 基于开盘价。当两条均线在完成的 K 线上发生交叉，且动量指标确认方向时入场，并通过固定点数的止盈距离复制原始 MT4 策略的平仓方式。

## 交易思想

1. **收盘价 EMA** 捕捉最新的价格变化，作为触发信号的敏捷曲线。
2. **开盘价 EMA** 变化更平滑，为判断交叉提供基准。
3. **动量指标（周期 14）** 衡量价格相对于 100 的偏离程度。只有当动量偏离大于 `MomentumFilter` 且继续朝同一方向增强时才允许交易。
4. **止盈距离** 通过 `TakeProfitPoints` 指定，乘以合约的最小价格跳动 `PriceStep` 得到目标价位。

## 入场条件

- **做多**
  - 当前已完成 K 线中，快速 EMA 上穿慢速 EMA，而上一根 K 线上快速 EMA 位于慢速 EMA 下方或相等。
  - 动量值减去 100 后大于 `MomentumFilter`，并且高于上一根 K 线的动量值。
  - 若存在空头仓位，会先买入平仓，然后按照 `Volume + |Position|` 的数量开立新的多头仓位。
- **做空**
  - 当前已完成 K 线中，快速 EMA 下穿慢速 EMA，而上一根 K 线上快速 EMA 位于慢速 EMA 上方或相等。
  - 动量值减去 100 后小于 `-MomentumFilter`，并且低于上一根 K 线的动量值。
  - 若存在多头仓位，会先卖出平仓，然后按照 `Volume + |Position|` 的数量开立新的空头仓位。

## 离场条件

- 当价格触及止盈价位 (`TakeProfitPoints * PriceStep`) 时平仓。
- 反向信号出现时会立即反手，新的订单数量同时覆盖已有仓位。

## 参数说明

| 参数 | 含义 | 默认值 |
| --- | --- | --- |
| `FastPeriod` | 收盘价 EMA 的周期。 | 13 |
| `SlowPeriod` | 开盘价 EMA 的周期。 | 34 |
| `MomentumPeriod` | 动量指标的计算周期。 | 14 |
| `MomentumFilter` | 动量偏离 100 的最小阈值。 | 0.1 |
| `TakeProfitPoints` | 止盈距离（点），乘以 `PriceStep` 得到价格距离。 | 200 |
| `CandleType` | 使用的蜡烛图类型，默认 15 分钟。 | 15 分钟 |
| `Volume` | 基础下单数量，继承自基类属性。 | 1 |

## 实现细节

- 只在 `CandleStates.Finished` 的收盘 K 线上处理信号。
- 使用 `SubscribeCandles` 订阅所选蜡烛数据，通过高层 API 将 EMA 与动量指标绑定。
- 为了模拟 MT4 中 `PRICE_OPEN` 的行为，慢速 EMA 在回调中手动输入开盘价。
- 止盈逻辑使用当根 K 线的最高价和最低价检测是否触发点数目标。
- 在启动阶段调用 `StartProtection()`，以避免策略启动时存在未预期仓位。
