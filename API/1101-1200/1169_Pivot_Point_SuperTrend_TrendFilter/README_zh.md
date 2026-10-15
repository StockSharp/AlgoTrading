# 枢轴点 SuperTrend 趋势过滤 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略将基于枢轴点的 SuperTrend 与另一个 SuperTrend 趋势过滤器及移动平均线结合使用。当趋势翻转或枢轴 SuperTrend 在指定日期范围内发出信号时进行交易。

## 细节

- **入场条件**：
  - 多头：价格 SuperTrend 转为上行且收盘价高于收盘价 SMA，或枢轴 SuperTrend 在日期窗口内转为上行。
  - 空头：价格 SuperTrend 转为下行且收盘价低于 SMA，或枢轴 SuperTrend 在日期窗口内转为下行。
- **出场条件**：
  - 任一下行切换全额平多仓，任一上行切换全额平空仓，不受 SMA 或日期限制。
  - 只有反向入场条件满足时才反转。两个指标同时发出相反信号时只平仓，不新开仓。
- **止损**：无
- **默认值**：
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
  - `CandleType` = 5 分钟
  - `StartDate` / `EndDate` = UTC `DateTime.MinValue` / `DateTime.MaxValue`
- **过滤器**：
  - 类别：Trend
  - 方向：双向
  - 指标：Pivot, SuperTrend, SMA
  - 止损：无
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：可选
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

## 计算与执行

仅使用已完成的蜡烛。枢轴高点/低点必须严格高于/低于左右各 `PivotPeriod` 根蜡烛，且右侧蜡烛全部完成后才确认。如果同一根蜡烛同时符合两个条件，高点优先。首个枢轴初始化 `center`，后续按 `(2 * center + pivot) / 3` 更新。

枢轴带使用 Wilder ATR：前 `AtrPeriod` 个真实波幅取平均，此后以 `1 / AtrPeriod` 权重平滑。基础支撑/阻力为 `center ± Factor * ATR`。前收盘价高于前支撑时，支撑取基础值与前值的最大值，否则重置；前收盘价低于前阻力时，阻力取最小值，否则重置。收盘价严格高于前阻力则转为上行，严格低于前支撑则转为下行，否则保留方向。初始方向为上行，初始化不产生信号。

独立价格过滤器使用 StockSharp 原生 SuperTrend（`TrendAtrPeriod`、`TrendMultiplier`）。SMA 包含当前收盘价。只有方向切换产生信号，两个 ATR、SMA 和枢轴中心都必须就绪。`StartDate <= candle.OpenTime <= EndDate` 只限制枢轴入场。日期必须为有序 UTC 值。入场目标仓位为 `Volume`，反转订单量为 `Volume + abs(Position)`，平仓量为 `abs(Position)`。没有冷却时间或保护止损。

枢轴概念来源：[LonesomeTheBlue 的 Pivot Point Supertrend](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/)。上述规则定义本示例的双向模型。
