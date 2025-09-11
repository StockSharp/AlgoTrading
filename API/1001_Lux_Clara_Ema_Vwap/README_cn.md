# Lux Clara EMA + VWAP 策略
[English](README.md) | [Русский](README_ru.md)

Lux Clara EMA + VWAP 策略利用快慢 EMA 的金叉/死叉，并结合 VWAP 和时间窗口过滤。当快 EMA 上穿慢 EMA 且慢 EMA 在 VWAP 之上并处于指定时段内时开多仓；相反条件下开空仓。EMAs 反向交叉时平仓。

## 细节

- **入场条件**：
  - 快 EMA 上穿慢 EMA，慢 EMA 高于 VWAP，当前时间在交易时段内。
  - 做空：快 EMA 下穿慢 EMA，慢 EMA 低于 VWAP，当前时间在交易时段内。
- **方向**：多空皆可。
- **出场条件**：
  - EMA 反向交叉。
- **止损**：无。
- **默认参数**：
  - `FastEmaLength` = 8
  - `SlowEmaLength` = 50
  - `StartTime` = 07:30
  - `EndTime` = 14:30
  - `CandleType` = 5 分钟
- **过滤器**：
  - 类型：趋势跟随
  - 方向：多空
  - 指标：EMA、VWAP
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：低
