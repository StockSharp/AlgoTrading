# Proffessor v3 策略
[English](README.md) | [Русский](README_ru.md)

## 概述

该策略是 MetaTrader 专家顾问 *Proffessor v3* 在 StockSharp 高层 API 中的完整实现。
核心思想保持不变：利用 ADX 判断市场状态，并在当前头寸周围构建保护与加仓网格。

- **指标**：14 周期的平均趋向指数 (ADX) 以及 +DI/-DI 线。
- **模式**：ADX 低于阈值视为盘整，高于阈值视为趋势。
- **委托**：先建立一个市价仓位，再在价格附近布置对称的挂单以对冲、加仓或均值回归。
- **离场**：未实现盈亏达到预设阈值时关闭全部仓位并撤销所有挂单。
- **时段**：仅在指定的小时区间内允许开仓。

## 交易逻辑

### 状态判定
1. 订阅所选蜡烛类型并计算 ADX。
2. 将 ADX 信号按照 `BarOffset` 延后若干根收盘蜡烛，复刻 MQL 中
   `CopyBuffer(handle, shift)` 的行为。
3. 当没有仓位时，根据延后的 ADX 值做出判定：
   - **盘整多头**：`ADX < AdxFlatLevel` 且 `+DI > -DI`。
   - **盘整空头**：`ADX < AdxFlatLevel` 且 `+DI < -DI`。
   - **趋势多头**：`ADX ≥ AdxFlatLevel` 且 `+DI > -DI`。
   - **趋势空头**：`ADX ≥ AdxFlatLevel` 且 `+DI < -DI`。

### 网格布置
开仓后使用基础手数，在当前价格周围放置对称的挂单。所有距离均以“点”
表示，与原始 MQL 代码完全一致，并按品种最小价位进行缩放。

- **盘整多头**：买入开仓，向下放置保护性 sell-stop，同时在下方挂买单、上方挂卖单。
- **盘整空头**：卖出开仓，向上放置保护性 buy-stop，并在下方挂买单覆盖空头，在上方挂卖单再次做空。
- **趋势多头**：买入开仓，sell-stop 用于对冲，buy-stop 用于突破加仓。
- **趋势空头**：卖出开仓，sell-stop 顺势跟踪，buy-stop 防止急剧反转。

每个层级的距离为 `GridStep + GridDeltaIncrement * level / 2`，挂单手数依据
`LotMultiplier` 和 `LotAddition` 调整后，再根据交易所的数量步长与上下限进行规范化。

### 风险管理
- 未实现盈亏通过仓位的平均价与最新收盘价计算。
- 当盈亏超过 `ProfitTarget` 或低于 `LossLimit`（不为 0 时有效）即触发全平。
- 交易窗口之外会跳过所有信号，实现方式与原始 `Time()` 函数一致。

## 实现细节

- 挂单的买卖价使用最近一根蜡烛的收盘价加减半个价位来近似，
  以便在蜡烛驱动的环境中复刻原始的逐笔逻辑。
- “点” 的数值依据品种价格步长进行缩放，并对三位或五位报价作出与
  MQL 变量 `m_adjusted_point` 相同的修正。
- 所有委托在发送前都会按照交易所的价格步长、最小与最大数量进行规范化。
- 仅处理已完成的蜡烛，避免提前触发信号。

## 参数

| 参数 | 说明 |
|------|------|
| `Volume` | 市价开仓的基础手数。 |
| `LotMultiplier` | 对每个挂单手数应用的倍数。 |
| `LotAddition` | 在倍数之后额外增加的手数。 |
| `MaxLevels` | 每侧最多的网格层数。 |
| `GridDeltaIncrement` | 深层网格的额外间隔（点）。 |
| `GridInitialOffset` | 第一张保护性挂单的距离（点）。 |
| `GridStep` | 相邻网格层之间的基础间隔（点）。 |
| `ProfitTarget` | 触发全部平仓的未实现利润值。 |
| `LossLimit` | 触发全部平仓的未实现亏损值（0 表示关闭此功能）。 |
| `AdxFlatLevel` | 区分盘整与趋势的 ADX 阈值。 |
| `BarOffset` | ADX 信号延后的收盘蜡烛数量。 |
| `StartHour` | 交易窗口开始的小时（UTC）。 |
| `EndHour` | 交易窗口结束的小时（UTC）。 |
| `CandleType` | 用于计算的蜡烛数据类型。 |

