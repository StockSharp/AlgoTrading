# MoStAsHaR15 枢轴线策略

## 概述
该策略使用 StockSharp 高层 API 复刻 MetaTrader 4 的“MoStAsHaR15 FoReX - Pivot Line”专家顾问。策略保留了原始的日内地板枢轴网格，并结合 ADX、EMA 差值以及 MACD 柱（OsMA）等动量过滤器。交易逻辑基于 1 小时 K 线，另外订阅上一根完整的日线用于在每根小时 K 线更新枢轴水平。

## 交易逻辑
- **枢轴计算**：利用上一日的最高价、最低价和收盘价计算经典枢轴（P）、三层阻力位（R1–R3）、三层支撑位（S1–S3）以及六个中间点（M0–M5）。当前 K 线的收盘价会与枢轴梯度比较以确定所处区间。原始 EA 中将 M5 和 R3 之间的区间映射回 S3/M0 的特殊处理被完整保留。
- **距离过滤**：只有当到最近止盈边界的距离超过 `MinimumDistancePips`（默认 14 点）时才允许交易，完全对应原代码中的 `dif1`/`dif2` 判断。
- **多头条件**：
  - ADX 主线高于 `AdxThreshold`（20），并且 +DI 同时走强且高于 −DI。
  - 收盘价 EMA 至少比开盘价 EMA 高 `EmaSpreadPips`（5 点），上一根 K 线已经具备同样的多头排序。
  - MACD 柱（OsMA）相较上一根 K 线上升。
- **空头条件**：与多头相反，要求 −DI 走强、EMA 出现空头差值、OsMA 下降。
- 每次只持有一个净头寸，通过 `BuyMarket()` 和 `SellMarket()` 以市价方式开仓。

## 仓位管理
- **止损**：可选，位于入场价下方/上方 `StopLossPips` 点。设置为 `0` 可关闭初始止损。
- **止盈**：固定在开仓时所在区间的支撑或阻力边界上。
- **移动止损**：当价格相对入场价向有利方向突破 `TrailingStopPips + TrailingStepPips` 后，止损会以 `TrailingStopPips` 的距离进行跟踪。启用移动止损时必须保证步长为正值。
- 若在一根 K 线内触发止损、移动止损或止盈，策略会在该根 K 线的评估阶段平仓。

## 策略参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `HourlyCandleType` | 执行逻辑所使用的小时级别 K 线。 | 1 小时 |
| `DailyCandleType` | 计算枢轴所用的日线序列。 | 1 天 |
| `StopLossPips` | 初始止损距离（点）。设为 `0` 关闭。 | 20 |
| `TrailingStopPips` | 移动止损距离（点）。 | 10 |
| `TrailingStepPips` | 移动止损触发所需的最小盈利点数。启用移动止损时必须 > 0。 | 5 |
| `MinimumDistancePips` | 入场前到最近枢轴边界的最小点数。 | 14 |
| `EmaSpreadPips` | 收盘价 EMA 与开盘价 EMA 的最小差值。 | 5 |
| `AdxThreshold` | 触发信号的最小 ADX 值。 | 20 |
| `AdxPeriod` | ADX 指标周期。 | 14 |
| `EmaClosePeriod` | 应用于收盘价的 EMA 周期。 | 5 |
| `EmaOpenPeriod` | 应用于开盘价的 EMA 周期。 | 8 |
| `MacdFastPeriod` | MACD 快速 EMA 周期（OsMA 分子）。 | 12 |
| `MacdSlowPeriod` | MACD 慢速 EMA 周期。 | 26 |
| `MacdSignalPeriod` | MACD 信号线 EMA 周期。 | 9 |

## 转换说明
- 所有指标仅在完整 K 线上计算，并且不保存历史数组，所有状态通过标量字段维护，符合仓库指南。
- 点值依据证券的 `PriceStep` 和小数位数推导。对于 3 位或 5 位报价的品种，会自动采用与 MetaTrader 相同的“迷你点”处理。
- 针对 M5→R3 区间的止盈映射故意保持为 S3/M0，以忠实呈现原始逻辑。
- 策略代码中的注释全部使用英文，满足项目要求。

## 使用建议
- 根据交易品种的交易时段调整 K 线类型，尤其是每日收盘时间与常规市场不同的资产。
- 由于止损和止盈在收盘后才判定，相比 MetaTrader 的逐笔执行可能出现额外滑点。
- 针对不同波动性的市场，可适当调整 `MinimumDistancePips` 和 `EmaSpreadPips` 等过滤参数。
