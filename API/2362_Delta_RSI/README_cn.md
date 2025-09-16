# Delta RSI 策略

该策略基于 **Delta RSI** 指标，通过比较两个不同周期的 RSI：

- 快速 RSI 对价格变化反应敏感。
- 慢速 RSI 用于趋势过滤。

当出现 **Up** 信号后的下一根K线时，如果慢速 RSI 高于 `Level` 且快速 RSI 高于慢速 RSI，则开多。

当出现 **Down** 信号后的下一根K线时，如果慢速 RSI 低于 `100 - Level` 且快速 RSI 低于慢速 RSI，则开空。

可以分别启用或禁用多头和空头的开仓与平仓。

## 参数

`FastPeriod`、`SlowPeriod`、`Level`、`BuyPosOpen`、`SellPosOpen`、`BuyPosClose`、`SellPosClose`、`CandleType`。
