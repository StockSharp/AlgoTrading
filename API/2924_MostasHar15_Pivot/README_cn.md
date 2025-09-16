# MostasHaR15 枢轴策略

## 概述
该策略在 StockSharp 高层 API 上重现了原始 **MostasHaR15 Pivot** MQL5 智能交易系统的逻辑。策略结合了上一交易日的地板枢轴线、ADX 趋势强度、EMA 差值以及 MACD 直方图（OsMA）过滤器。默认情况下使用 1 小时 K 线进行交易，同时在每根 K 线收盘时读取上一根完成的日线来重新构建枢轴网格。

## 交易逻辑
- **枢轴网格**：利用上一日的最高价、最低价和收盘价计算主枢轴点 P、三条阻力线 R1–R3、三条支撑线 S1–S3 以及六个中点 M0–M5。当前 K 线收盘价与这些水平比较，以确定价格所在的支撑/阻力区间。策略保留了原始 EA 的特殊处理，即当价格位于 M5 与 R3 之间时，仍然使用 S3/M0 作为目标区间。
- **距离过滤**：只有当到最近枢轴目标的距离超过 `MinimumDistancePips`（默认 14 点）时才允许开仓，这与 EA 中的 `dif1`/`dif2` 条件一致。
- **多头条件**：
  - ADX 主线高于 `AdxThreshold`（20），且 +DI 同时向上并高于 −DI。
  - 5 周期收盘价 EMA 至少比 8 周期开盘价 EMA 高 `EmaSlopePips`（5 点），并且上一根 K 线也呈现同样的多头 EMA 排列。
  - MACD 直方图值较上一根 K 线增加。
- **空头条件**：完全镜像多头条件，要求 −DI 强势、EMA 向下张口以及 MACD 直方图下降。
- 策略始终保持单净头寸，通过 `BuyMarket()` / `SellMarket()` 以市价执行订单。

## 仓位管理
- **止损**：可选，距离入场价 `StopLossPips` 点。参数设置为 `0` 时禁用初始止损，与原始 EA 保持一致。
- **止盈**：固定在开仓时价格所处的枢轴区间边界。
- **移动止损**：完全复刻 EA 的拖尾逻辑。当浮动盈利超过 `TrailingStopPips + TrailingStepPips` 时，将止损提高/降低到距离当前价 `TrailingStopPips` 的位置。将 `TrailingStopPips` 设为 `0` 可关闭拖尾。
- 止损、拖尾或止盈任意条件在某根 K 线内被触发时，策略会在该 K 线结束时平仓。

## 参数说明
| 参数 | 含义 | 默认值 |
|------|------|--------|
| `CandleType` | 用于交易的主图 K 线类型。 | 1 小时时间框架 |
| `DailyCandleType` | 计算枢轴所用的日线类型。 | 1 天时间框架 |
| `StopLossPips` | 止损距离（点）。为 0 时禁用。 | 20 |
| `TrailingStopPips` | 移动止损距离（点）。 | 5 |
| `TrailingStepPips` | 更新移动止损所需的最小盈利（点）。启用拖尾时必须大于 0。 | 5 |
| `MinimumDistancePips` | 开仓前到最近枢轴目标的最小距离（点）。 | 14 |
| `EmaSlopePips` | 收盘价 EMA 与开盘价 EMA 之间的最小差值（点）。 | 5 |
| `AdxThreshold` | 多空共用的 ADX 最小阈值。 | 20 |
| `AdxPeriod` | ADX 指标周期。 | 14 |
| `EmaClosePeriod` | 收盘价 EMA 周期。 | 5 |
| `EmaOpenPeriod` | 开盘价 EMA 周期。 | 8 |
| `MacdFastPeriod` | MACD 快线 EMA 周期。 | 12 |
| `MacdSlowPeriod` | MACD 慢线 EMA 周期。 | 26 |
| `MacdSignalPeriod` | MACD 信号线 EMA 周期。 | 9 |

## 转换备注
- 保留了原始 EA 中 M5–R3 区间对应 S3/M0 的特殊映射，以确保回测结果一致。
- 所有指标计算都基于已完成的 K 线，不创建额外的历史集合，状态均存放在字段中，符合仓库规范。
- 代码中的注释全部使用英文，以符合仓库要求。

## 使用建议
- 在不同交易时段的市场上使用时，请调整 `CandleType` 与 `DailyCandleType` 以匹配可用数据。
- 由于止损和拖尾在 K 线收盘时执行，与逐笔执行的原版 EA 相比，在高速行情中可能出现额外滑点。
