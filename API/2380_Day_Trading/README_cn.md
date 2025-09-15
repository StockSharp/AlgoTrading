# Day Trading 策略
[English](README.md) | [Русский](README_ru.md)

该策略在5分钟K线下交易，结合 Parabolic SAR、MACD (12,26,9)、Stochastic Oscillator (5,3,3) 和 Momentum (14)。只有当所有指标一致时才入场。

- **做多**：SAR 跌至价格下方且先前高于当前，Momentum < 100，MACD 线低于信号线，随机指标 %K < 35。
- **做空**：SAR 升至价格上方且先前低于当前，Momentum > 100，MACD 线高于信号线，随机指标 %K > 60。

出现反向条件时平仓。风险控制使用跟踪止损和可选的止盈。

## 参数
- **Volume** – 下单数量。
- **Take Profit** – 止盈点数。
- **Trailing Stop** – 跟踪止损点数。
- **Candle Type** – 使用的K线类型（默认5分钟）。
