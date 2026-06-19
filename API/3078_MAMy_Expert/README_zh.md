# MAMy Expert 策略

## 概述
- 将 Victor Chebotariov 的 MetaTrader5 “MAMy Expert” 智能交易系统移植到 StockSharp 的高级策略 API。
- 复现原始自定义指标，对比三种不同价格来源的移动均线（开盘、收盘、加权价）。
- 仅在完成的 K 线出现信号，并且一次只持有一个方向的净头寸，行为与原始 EA 保持一致。

## 指标基础
- 策略使用相同的周期和算法构建三条移动均线：
  - `MA(close)`：基于 K 线收盘价计算。
  - `MA(open)`：基于 K 线开盘价计算。
  - `MA(weighted)`：基于加权价格 `(High + Low + 2 × Close) / 4` 计算。
- `MaType` 参数控制平滑算法（简单、指数、平滑、加权 LWMA），与 MetaTrader 的 `MODE_*` 选项对应。
- “平仓缓冲”由差值 `MA(close) − MA(weighted)` 组成。
- 只有在均线形成趋势结构时才会生成“开仓缓冲”：
  - **下跌结构**：`MA(close)` 与 `MA(weighted)` 同时下降，收盘均线位于加权均线之下，两者都在开盘均线之下，并且平仓缓冲继续下降。
  - **上涨结构**：`MA(close)` 与 `MA(weighted)` 同时上升，收盘均线位于加权均线之上，两者都在开盘均线之上，并且平仓缓冲继续上升。
  - 满足任一结构时，开仓缓冲取 `(MA(weighted) − MA(open)) + (MA(close) − MA(weighted))`；否则被重置为 0。
- 当新的正开仓缓冲出现且平仓缓冲转为负值时，按照原指标的处理方式，将平仓缓冲强制归零。

## 信号逻辑
- **入场条件**
  - **买入**：开仓缓冲上穿 0（前值 ≤ 0，当前值 > 0）。
  - **卖出**：开仓缓冲下穿 0（前值 ≥ 0，当前值 < 0）。
  - 仅在没有持仓时评估入场信号。
- **离场条件**
  - **平多**：平仓缓冲下穿 0（前值 ≥ 0，当前值 < 0）。
  - **平空**：平仓缓冲上穿 0（前值 ≤ 0，当前值 > 0）。
  - 离场优先于入场，因此策略不会同时持有多空头寸。
- 所有订单均以参数 `TradeVolume` 指定的数量市价成交。调用 `StartProtection()` 与 StockSharp 样例中的安全处理一致。

## 图表与数据流
- 订阅 `CandleType` 指定的时间框架，仅处理完成态 K 线。
- 在图表中绘制价格 K 线与三条移动均线，并标注成交，提供与原指标相同的可视化反馈。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | 用于指标与信号计算的主要时间框架。 |
| `MaPeriod` | `int` | `3` | 三条移动均线的长度。 |
| `MaType` | `MaCalculationType` | `Weighted` | 平滑算法（Simple、Exponential、Smoothed、Weighted）。 |
| `TradeVolume` | `decimal` | `1` | 每次市价单的下单量。 |

## 实现说明
- 使用 StockSharp 的高级 `SubscribeCandles().Bind(...)` 工作流及内置移动均线指标，仅保存生成信号所需的最近数据。
- 只有在所有指标完全形成并且策略处于可交易状态 (`IsFormedAndOnlineAndAllowTrading()`) 时才评估信号。
- 持仓期间忽略新的入场信号，与源 EA 的运行方式保持一致。
