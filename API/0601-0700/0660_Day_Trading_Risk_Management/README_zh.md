# 日内交易策略（含风险管理）
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略在快速 EMA 上穿慢速 EMA 且 RSI 低于超买水平时做多，在快速 EMA 下穿慢速 EMA 且 RSI 高于超卖水平时做空。风险管理通过固定百分比的止盈和止损实现。

### 精确交易与保护规则

默认：`CandleType=1分钟`、`FastEmaPeriod=20`、`SlowEmaPeriod=50`、`RsiLength=14`、`RsiOverbought=70`、`RsiOversold=30`、`TakeProfitPercent=2`、`StopLossPercent=1`。在**已完成K线**上，如果先前 `EMA20 <= EMA50`、当前 `EMA20 > EMA50` 且 `RSI14 < 70` 则做多；如果先前 `EMA20 >= EMA50`、当前 `EMA20 < EMA50` 且 `RSI14 > 30` 则做空。可从空仓入场或以 `Volume + abs(Position)` 反转相反持仓。**没有RSI确认就不进场**。成交后使用**原生固定2%止盈与1%止损**，不启用追踪止损；保护随市场价格更新，不依赖K线收盘。旧版本只有EMA120/450交叉、缺少RSI和保护。原README未提供数值默认值，以上为明确选定的实现配置。



## 参数
- K线类型
- 快速 EMA 长度
- 慢速 EMA 长度
- RSI 周期
- RSI 超买水平
- RSI 超卖水平
- 止盈百分比
- 止损百分比
