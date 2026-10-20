# BBSR Extreme

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略结合布林带回归、EMA 斜率过滤和 ATR 保护。`C,L,U,E` 分别为原始收盘价、下轨、上轨和 EMA；`p` 为前一根指标已形成的蜡烛。

## 交易规则

做多：`C[p] < L[p] && C > L && E > E[p]`；做空：`C[p] > U[p] && C < U && E < E[p]`。等号不触发。仅空仓时以 `Volume` 入场，不加仓、不反转，也无固定等待蜡烛数。

入场发单前固定信号蜡烛的 `A=ATR`。本地原生保护按实际成交价计算：多头止损/目标为 `fill-2A` / `fill+3A`，空头为 `fill+2A` / `fill-3A`。倍数可调，后续 ATR 不改变保护距离。bid/ask 订阅支持在信号蜡烛之间以市价关闭实际成交数量。

仅在指标充分预热后使用已完成的蜡烛生成入场信号。持仓持续管理至退出；未完成的市价订单会阻止重复提交。

布林带采用滚动收盘价窗口的总体标准差，包含当前已完成蜡烛。

## 参数

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `20` |
| `BollingerMultiplier` | `2` |
| `MaLength` | `7 (EMA)` |
| `AtrLength` | `14 (Wilder)` |
| `AtrStopMultiplier` | `2` |
| `AtrProfitMultiplier` | `3` |
| `CandleType` | `5m` |
