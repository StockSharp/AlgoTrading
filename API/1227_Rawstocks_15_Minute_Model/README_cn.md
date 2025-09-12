# Rawstocks 15 Minute Model 策略

Rawstocks 15 Minute Model 使用摆动订单块和斐波那契回撤水平，在日内时段内交易。

## 工作原理
- 通过 ATR 过滤器识别摆动高点和低点。
- 生成多头和空头订单块并计算 61.8% 与 79% 斐波那契水平。
- 当价格触及多头订单块并在截止时间前收于斐波那契水平之上时做多。
- 当价格测试空头订单块并收于斐波那契水平之下时做空。
- 每天 16:30（美东时间）强制平仓。

## 参数
- Start Hour
- Start Minute
- Last Entry Hour
- Last Entry Minute
- Force Close Hour
- Force Close Minute
- Fib Level (%)
- Min Swing Size (%)
- Risk/Reward

### 指标
- Average True Range
