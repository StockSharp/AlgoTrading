# MTF Seconds Values JD 策略

该策略演示如何基于自定义秒级别聚合构建多时间框架K线，并计算简单移动平均线。当收盘价与均线发生交叉时发出交易信号。

## 参数

- `SecondsTimeframe` – 秒级K线的长度。
- `AverageLength` – 简单移动平均周期。
