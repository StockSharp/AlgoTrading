# Refined SMA EMA Crossover with Ichimoku and 200 SMA Filter
[English](README.md) | [Русский](README_ru.md)

该策略结合 Ichimoku 云和 200 期 SMA 来过滤 SMA 与 EMA 的交叉。当 SMA 上穿 EMA 且价格位于云层和 200 SMA 之上时做多；当 SMA 下穿 EMA 且价格位于云层和 200 SMA 之下时做空。

## 细节

- **做多条件：** SMA 上穿 EMA，价格高于 Ichimoku 云和 200 SMA。
- **做空条件：** SMA 下穿 EMA，价格低于 Ichimoku 云和 200 SMA。
- **退出：** 反向信号。
- **指标：** Ichimoku 云、SMA、EMA、200 SMA。
