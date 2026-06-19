# Pineconnector Strategy Template

该策略展示如何连接任意指标来产生交易信号。示例使用两条移动平均线，当快线向上穿越慢线时做多，相反情况做空。

## 参数
- **Fast Length** – 快速移动平均线周期。
- **Slow Length** – 慢速移动平均线周期。
- **Candle Type** – 计算所用的K线类型。
