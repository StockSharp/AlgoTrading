# 相对蜡烛策略
[English](README.md) | [Русский](README_ru.md)

该策略将当前品种的价格变化与指定指数进行比较。
它计算开盘价、最高价、最低价、收盘价的相对值，并对相对收盘价应用移动平均。

## 参数

- **IndexSymbol** – 用于比较的指数，默认 `IXIC`。
- **AverageCloseLength** – 相对收盘价移动平均的周期，默认 `10`。
- **AverageZoomFactor** – 相对收盘价平均值的缩放系数，默认 `5`。
- **CandleType** – 要处理的K线类型。
