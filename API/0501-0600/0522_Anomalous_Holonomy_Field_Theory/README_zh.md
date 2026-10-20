# Anomalous Holonomy Field Theory

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

六部分技术信号：固定 EMA20/EMA50、Wilder RSI14、MACD(12,26,9)、Wilder ATR 及按波动率归一化的 ROC。`C` 为原始收盘，`A=ATR`；`clip(x,k)=max(-k,min(k,x))`。窗口包含当前已完成蜡烛。

## 交易规则

`T`：高于两条 EMA 时 +1.5，仅高于 EMA20 时 +1；低于两条时 -1.5，仅低于 EMA20 时 -1，否则 0。蜡烛 VWAP 为最近 `VwapPeriod` 根的 `sum(((High+Low+Close)/3)*Volume)/sum(Volume)`。`V=clip((C-VWAP)/A,2)`。令 `B=T+V`：仅当 `B>0`，RSI 小于 30 加 +1.5，小于 40 加 +0.5；仅当 `B<0`，RSI 大于 70 加 -1.5，大于 60 加 -0.5；其余 `R=0`。

`M=clip((MACD-EMA9(MACD))/A,1)`；`Q=clip((C-C[RocPeriod 根前])/A,1)` 等于使用相同历史价格基准的 ROC/ATR%。总信号 `S=T+V+R+M+Q`。仅空仓时，`S>=SignalThreshold` 买入 `Volume`，`S<=-SignalThreshold` 卖出。预热不足、ATR 非正或成交量为零时不入场。连续入场蜡烛索引之差必须严格大于 `CooldownBars`。

每次入场前固定 `ATR*AtrStopMultiplier` 距离，本地原生市价止损以实际成交价为基准，并在蜡烛之间响应 bid/ask。默认 2 ATR。持仓通过保护退出，不随反向分数加仓或反转；入场等待期不会延后保护。无止盈目标。

仅在指标充分预热后使用已完成的蜡烛生成入场信号。持仓持续管理至退出；未完成的市价订单会阻止重复提交。

## 参数

| Parameter | Default |
|---|---|
| `SignalThreshold` | `2` |
| `CooldownBars` | `50` |
| `AtrLength` | `14` |
| `AtrStopMultiplier` | `2` |
| `RocPeriod` | `10` |
| `VwapPeriod` | `20` |
| `CandleType` | `5m` |
