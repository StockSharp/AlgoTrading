# Quantum Sentiment Flux 初学者策略

当快速 EMA 上穿慢速 EMA 且差值超过基于 ATR 的阈值时做多，反向信号时做空。价格回撤 ATR*倍数或达到两倍 ATR 的目标时平仓。冷却参数限制交易频率。

## 参数
- 蜡烛类型
- 快速 EMA 长度
- 慢速 EMA 长度
- ATR 周期
- ATR 乘数
- EMA 强度阈值
- 冷却柱数
- 交易数量
