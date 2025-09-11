# MathStatisticsKernelFunctions 策略

实现多个统计核函数，当所选核函数输出高于或低于 0.5 时进行交易。

## 参数
- **Kernel** – 核函数名称 (`uniform`, `triangle`, `epanechnikov`, `quartic`, `triweight`, `tricubic`, `gaussian`, `cosine`, `logistic`, `sigmoid`).
- **Bandwidth** – 核函数带宽。
- **Candle Type** – K线周期。
