# Silver Trend 策略
[English](README.md) | [Русский](README_ru.md)

基于自定义 SilverTrend 指标的趋势跟随策略。该指标利用指定周期内的最高价和最低价以及风险参数构建动态价格通道。当价格突破通道并导致趋势方向改变时产生交易信号。

## 详情

- **入场**：指标转为上升趋势时买入，转为下降趋势时卖出。
- **出场**：出现相反信号时反向开仓。
- **指标**：Highest、Lowest、SimpleMovingAverage（用于 SilverTrend 计算）。
- **止损**：无。
- **默认值**：
  - `Ssp` = 9 — 计算通道的K线数量。
  - `Risk` = 3 — 缩小通道宽度的百分比。
  - `CandleType` = 1 小时K线。
- **方向**：做多和做空。

SilverTrend 指标计算 `Ssp + 1` 根K线的高低价平均范围，并在 `Ssp` 根K线内寻找最高点和最低点。通道边界如下：

```
smin = minLow + (maxHigh - minLow) * (33 - Risk) / 100
smax = maxHigh - (maxHigh - minLow) * (33 - Risk) / 100
```

若收盘价低于 `smin`，认为趋势转为空头；若高于 `smax`，认为趋势转为多头。趋势翻转时生成信号，策略会立即反向持仓。
