# Refined SMA EMA Crossover with Ichimoku and 200 SMA Filter
[Русский](README_ru.md) | [中文](README_cn.md)

Combines a short SMA/EMA crossover with Ichimoku Cloud and 200-period SMA filters. Goes long when SMA crosses above EMA above the cloud and the 200 SMA. Sells when SMA crosses below EMA below both the cloud and 200 SMA.

## Details

- **Entry Criteria:**
  - **Long:** SMA crosses above EMA, price above Ichimoku cloud, price above 200 SMA.
  - **Short:** SMA crosses below EMA, price below Ichimoku cloud, price below 200 SMA.
- **Exit Criteria:** reverse signal.
- **Indicators:** Ichimoku Cloud, SMA, EMA, 200 SMA.
