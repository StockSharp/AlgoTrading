# Pivot Percentile Trend 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略利用多周期百分位趋势强度并结合 SuperTrend 过滤器。当趋势强度为正且价格高于 SuperTrend 时做多；当趋势强度为负且价格低于 SuperTrend 时做空。

## 本示例的计算方法

七个窗口 `PercentileLength * 1..7` 使用 High/Low 的四分位数（75%/25%，nearest rank）。当前 High 严格高于上四分位数时该窗口计 +1，当前 Low 严格低于下四分位数时计 -1。当前蜡烛包含在每个窗口中。

所有窗口的票数之和就是趋势强度。强度为正且 Close 高于价格 SuperTrend 时做多；强度为负且 Close 低于该线时做空。反向持仓会完全反转至 `Volume`。中性强度或过滤条件不满足时保持持仓。必须等待所有窗口完成预热；没有信号冷却时间或保护止损。

默认值：`PercentileLength=40`、`AtrPeriod=10`、`AtrMultiplier=3`、`CandleType=5m`。SuperTrend 使用平台的 Wilder ATR 和价格带。
