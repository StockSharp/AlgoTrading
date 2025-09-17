# Simple EA MA plus MACD
[English](README.md) | [Русский](README_ru.md)

## 概览
本策略将 MetaTrader 5 的 **Simple EA MA plus MACD** 专家顾问移植到 StockSharp 高层 API。算法寻找满足两个条件的“信号柱”并等待突破：带有水平位移的移动平均线位于蜡烛最高价之下/之上，同时 MACD 柱线刚刚穿越零轴。当下一根蜡烛收盘价突破信号柱的高点或低点时，策略沿突破方向建仓。

完整流程与原始 EA 保持一致：

1. **信号识别**：在每根收盘蜡烛上检查上一根柱体。可配置的移动平均（默认 LWMA）基于选定的价格类型计算，要求在多头情况下 MA 连续两根蜡烛都低于最高价（空头则高于最高价）。同时 MACD 主线必须在前两根柱之间跨越零轴。
2. **信号确认**：一旦记录信号柱，策略等待下一根蜡烛收盘。收盘价高于信号柱高点触发买入，收盘价低于信号柱低点触发卖出；若价格重新回到信号柱区间内，则放弃该信号。
3. **仓位管理**：新开仓位继承以点数表示的止损、止盈与跟踪止损参数。所有距离通过合约的 `PriceStep` 转换为绝对价格；若标的精度为三位或五位小数，则按照外汇习惯乘以 10，与 MetaTrader 的“点”定义保持一致。

## 风险控制
- **止损 / 止盈**：每次蜡烛收盘都会检查点数距离是否被触发，满足条件时使用市价单平仓。
- **跟踪止损**：当浮盈超过 `TrailingStopPips + TrailingStepPips` 后，在最近极值后方布置跟踪价位；若价格回撤到该水平则立即平仓。将 `TrailingStepPips` 设为 0 可以在每个新极值时更新跟踪止损。
- **反向信号处理**：当出现相反方向的突破时，会发送一笔足够大的市价单，同时平掉现有仓位并建立新的反向仓位。

## 实现要点
- 移动平均提供与 MetaTrader 相同的平滑方式和价格源选项（Simple、Exponential、Smoothed、LinearWeighted 以及 Close/Open/High/Low/Median/Typical/Weighted）。
- `MaShift` 通过读取历史值重现 MT5 指标的水平偏移，在比较信号柱时使用提前若干根的 MA 数据。
- MACD 使用内置的 `MovingAverageConvergenceDivergence` 指标。策略只需 MACD 柱线（快慢 EMA 的差值），但仍保留信号线周期以保持参数兼容。
- 指标计算和数据订阅完全依赖 StockSharp 的高层 API，无需手动处理指标缓冲区或逐笔行情。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Volume` | `1` | 每次突破下单的成交量。 |
| `TakeProfitPips` | `50` | 止盈距离（点），通过 `PriceStep` 转换为绝对价格，设为 0 表示关闭。 |
| `StopLossPips` | `50` | 止损距离（点），设为 0 表示关闭。 |
| `TrailingStopPips` | `5` | 跟踪止损的基准距离（点）。 |
| `TrailingStepPips` | `5` | 每次推进跟踪止损所需的额外盈利（点）。 |
| `MaPeriod` | `100` | 用于验证信号柱的移动平均周期。 |
| `MaShift` | `0` | 移动平均的水平偏移，对应 MT5 的 `ma_shift` 参数。 |
| `MaMethod` | `LinearWeighted` | 移动平均类型（Simple、Exponential、Smoothed、LinearWeighted）。 |
| `MaAppliedPrice` | `Weighted` | 移动平均输入的价格类型（Close、Open、High、Low、Median、Typical、Weighted）。 |
| `MacdFastPeriod` | `12` | MACD 快速 EMA 周期。 |
| `MacdSlowPeriod` | `26` | MACD 慢速 EMA 周期。 |
| `MacdSignalPeriod` | `9` | MACD 信号线周期，保留用于与原始 EA 对齐。 |
| `MacdAppliedPrice` | `Weighted` | 提供给 MACD 的价格类型。 |
| `CandleType` | 1 小时周期 | 用于分析信号与风控的主时间框架。 |

## 使用建议
- 在连接端确保正确配置 `PriceStep` 等交易参数，否则点数换算会产生偏差。
- 若市场波动剧烈，可以增大 `TrailingStepPips` 以减少重复止损，也可以减小该值以获得更紧密的跟踪。
- 策略仅在蜡烛收盘后下单，因此突破需要持续到收盘才能被执行；缩短时间框架会提高交易频率，但也会引入更多噪声。
