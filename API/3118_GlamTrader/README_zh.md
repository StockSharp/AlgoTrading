# GlamTrader 策略

**GlamTrader 策略** 将 MetaTrader 专家顾问 `GlamTrader.mq5` 移植到 StockSharp 的高级 API。原版算法结合了位移移动平均线、Laguerre RSI 振荡器和 Awesome Oscillator，只在动量满足条件时打开单向仓位。移植版本完整保留决策流程与资金管理，并使用 StockSharp 提供的下单、图表和保护功能。

## 核心流程

1. 订阅 `CandleType` 指定的 K 线序列（默认 M15），所有指标都基于该时间框计算。
2. 按照 `AppliedPrice` 指定的价格源构建可配置的移动平均线，并通过 `MaShift` 向右位移若干柱，以复现 MetaTrader 中的缓冲区。
3. 在策略内部复刻四阶 Laguerre RSI 滤波器，`LaguerreGamma` 控制平滑系数，输出范围保持在 `[0, 1]`，与原始 `laguerre.mq5` 指标一致。
4. 计算 Awesome Oscillator（5/34 周期的中价 SMA 之差），并保存当前值与上一柱的值用于判断斜率。
5. 当且仅当没有持仓时：
   - **做多**：移动平均线位于当前收盘价之上，Laguerre RSI 大于 `0.15`，Awesome Oscillator 相比上一柱上升。
   - **做空**：移动平均线位于当前收盘价之下，Laguerre RSI 小于 `0.75`，Awesome Oscillator 相比上一柱下降。
6. 入场时，将止损/止盈距离从点数换算为价格偏移，自动针对三位或五位报价应用 `Point * 10` 的调整，完全复刻 MQL 的计算方式。
7. 持仓期间复制原始的追踪止损逻辑：当价格走出 `TrailingStopPips + TrailingStepPips` 的利润后，把止损锁定在距离当前价 `TrailingStopPips` 的位置；一旦蜡烛最高/最低触及止损或止盈水平就立即离场。

## 入场与出场

- 策略始终只持有一笔仓位，反向信号会等待当前仓位平仓后才生效。
- 做多需要价格向上突破位移均线、Laguerre RSI 脱离超卖区（`> 0.15`）并伴随 Awesome Oscillator 上升动量。
- 做空需要价格跌破位移均线、Laguerre RSI 脱离超买区（`< 0.75`）并伴随 Awesome Oscillator 下行动量。
- 止损与止盈通过比较蜡烛的最高/最低价来触发，因此即便逻辑在收盘时运行，也能捕捉到盘中触及。
- 追踪止损完全遵循 MQL 规则：只有在利润超过“止损距离 + 步长”时才会移动，且不会回撤。

## 参数说明

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(15).TimeFrame()` | 指标与信号使用的时间框。 |
| `TradeVolume` | `decimal` | `1` | 每次进场的下单数量。 |
| `StopLossBuyPips` | `decimal` | `50` | 多单止损距离（点）。 |
| `TakeProfitBuyPips` | `decimal` | `50` | 多单止盈距离（点）。 |
| `StopLossSellPips` | `decimal` | `50` | 空单止损距离（点）。 |
| `TakeProfitSellPips` | `decimal` | `50` | 空单止盈距离（点）。 |
| `TrailingStopPips` | `decimal` | `5` | 追踪止损距离（点），设置为 0 可关闭追踪功能。 |
| `TrailingStepPips` | `decimal` | `15` | 每次移动止损前所需的额外利润（点）。 |
| `MaPeriod` | `int` | `14` | 移动平均周期。 |
| `MaShift` | `int` | `1` | 移动平均向右位移的柱数。 |
| `MaMethod` | `MaMethod` | `LinearWeighted` | 移动平均类型（简单、指数、平滑或线性加权）。 |
| `AppliedPrice` | `AppliedPrice` | `Weighted` | 移动平均与 Laguerre 滤波使用的价格来源。 |
| `LaguerreGamma` | `decimal` | `0.7` | Laguerre 滤波平滑系数（0–1）。 |

## 使用建议

1. 选择目标证券，确保经纪模型提供 `PriceStep` 信息，并根据需要设置 `CandleType`。
2. 根据品种波动调整各项点数参数。移植版会自动通过 `PriceStep` 归一化距离，对五位报价自动乘以 10。
3. 图表辅助会在主图上绘制移动平均，并在独立区域显示 Awesome Oscillator，同时标注策略交易。
4. 启动策略后，它会自动设置止损、止盈，并按照原始 MQL 逻辑执行追踪止损。

## 其他说明

- Laguerre RSI 的实现完全对应 `laguerre.mq5`，包含 `CU/(CU+CD)` 的归一化步骤。
- Awesome Oscillator 使用 StockSharp 自带指标，无需手动复制缓冲区。
- 策略只在完成的蜡烛上运行，避免了因逐笔数据导致的重复绘制和不确定性。
