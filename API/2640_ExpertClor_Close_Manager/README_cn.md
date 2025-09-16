# ExpertClor Close Manager 策略

## 概述

ExpertClor Close Manager Strategy 是一个风险控制模块，用于监控已有仓位并在满足退出条件时平仓。该策略移植自 MetaTrader 的 *ExpertClor_v01* 智能交易顾问，其原始版本只负责管理仓位而不主动下单。StockSharp 版本保持同样的设计：策略不会开仓，只会跟踪当前仓位并发送市价单退出。

## 核心逻辑

1. **均线交叉退出**  
   在每根收盘 K 线上计算一条快线和一条慢线移动平均。快线从上向下穿越慢线时平掉多头仓位；快线从下向上穿越慢线时平掉空头仓位。信号基于最近两根已完成 K 线，与原始 MQL5 逻辑一致。

2. **ATR 追踪止损**  
   通过可调节周期与乘数的 Average True Range 指标生成 StopATR_auto 式的追踪止损。多头的止损为 `收盘价 − ATR × 乘数`，空头为 `收盘价 + ATR × 乘数`，止损只会向盈利方向收紧。

3. **保本移动**  
   当行情朝持仓方向运行指定点数后，止损移动至开仓价，实现保本保护。多头与空头独立计算，可与 ATR 追踪止损同时使用。

4. **仓位状态管理**  
   所有计算基于选定的 K 线序列。没有持仓时会清除内部止损值；每次强制平仓后都会重置追踪状态，避免下一笔交易使用旧数据。

## 使用的指标

- 可选类型与价格源的快均线（支持 SMA、EMA、SMMA、WMA）。
- 同样可配置的慢均线。
- Average True Range，用于波动性追踪止损。

## 参数

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `MaCloseEnabled` | 是否启用均线交叉退出。 | `true` |
| `AtrCloseEnabled` | 是否启用 ATR 追踪止损。 | `true` |
| `FastMaPeriod` | 快均线周期。 | `5` |
| `FastMaMethod` | 快均线类型（Simple、Exponential、Smoothed、Weighted）。 | `Exponential` |
| `FastPriceType` | 快均线使用的价格。 | `Close` |
| `SlowMaPeriod` | 慢均线周期。 | `7` |
| `SlowMaMethod` | 慢均线类型。 | `Exponential` |
| `SlowPriceType` | 慢均线使用的价格。 | `Open` |
| `BreakevenPips` | 移动到保本所需的点数。 | `0` |
| `AtrPeriod` | ATR 周期。 | `12` |
| `AtrTarget` | ATR 乘数，用于计算追踪止损。 | `2.0` |
| `CandleType` | 计算所用的 K 线类型。 | `5 分钟` |

## 使用建议

- 将策略应用于由其他系统负责开仓的标的，ExpertClor Close Manager 只负责退出。
- 请订阅与原版 EA 相同的时间框架，以保持信号一致性。
- `BreakevenPips` 会通过 `PriceStep` 转换为价格增量，设为 `0` 可关闭保本逻辑。
- ATR 指标尚未形成时只会根据均线交叉退出（如果已启用）。
- 平仓通过市价单执行，如需限制滑点请在交易连接或券商侧设置。

## 移植说明

- 自定义的 StopATR_auto 指标用标准 ATR 追踪逻辑实现。
- MQL5 中循环遍历持仓的部分改写为 StockSharp 的蜡烛订阅和高层 API。
- 代码内保留英文注释，便于后续阅读与维护。
