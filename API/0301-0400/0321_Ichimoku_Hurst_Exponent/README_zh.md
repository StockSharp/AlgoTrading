# 一目均衡表与赫斯特指数
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

趋势策略结合转换线与基准线交叉、一目均衡表云区，以及基于对数收益率的赫斯特过滤器。信号仅使用已完成的蜡烛。

## 计算

转换线 Tenkan 和基准线 Kijun 分别为各自窗口的 `(最高 High + 最低 Low) / 2`。未位移的先行带 A 是两线的平均值，先行带 B 是 `SenkouSpanBPeriod` 窗口的高低点中值。策略使用 StockSharp Ichimoku 生成的因果位移云线，位移为 `KijunPeriod`，并遵循其初始化缓冲规则；不读取未来蜡烛。

最近 `HurstPeriod` 个收盘价产生 `n = HurstPeriod - 1` 个收益率 `r[i] = ln(C[i] / C[i-1])`。令 `mu` 为均值、`X[k] = sum(r[i] - mu)`、`R = max(X) - min(X)`、`S` 为总体标准差，则 `H = ln(R / S) / ln(n)`。StockSharp HurstExponent 对这些收益率进行计算，从第一根完成的蜡烛开始积累。至少需要十一個收盘价、完整窗口和非零方差。非正收盘价会重启窗口；没有有效赫斯特值时禁止开仓。

## 交易规则

- **做多：** 当前空仓，收盘价严格高于两条云线，Tenkan 从 `<= Kijun` 上穿为 `> Kijun`，`H > HurstThreshold`，ATR 已形成且为正，冷却结束。
- **做空：** 当前空仓，收盘价严格低于两条云线且发生反向交叉；赫斯特、ATR 和冷却条件相同。
- **信号平仓：** 多头在收盘价低于两条云线或 Tenkan 下穿时全部平仓；空头按对称条件全部平仓。冷却期间仍允许退出，退出不依赖入场的赫斯特过滤器。
- **委托：** 按 `Volume` 市价开仓，按实际剩余持仓量退出。未完成的委托会阻止下一笔信号委托。先完成平仓，之后才允许重新入场。
- **冷却：** 每笔信号委托重置 `SignalCooldownBars`；经过相应数量的已完成蜡烛后允许再次开仓。

## 保护

Wilder ATR 使用 `AtrPeriod`。每次开仓固定信号蜡烛的 `StopLossAtr * ATR` 距离，以实际开仓成交价为基准。本地保护处理 Level1 更新，可在信号蜡烛之间通过市价委托平仓。后续 ATR 不改变该距离。`StopLossAtr = 0` 表示本次运行禁用保护。不设置止盈或移动止损。

## 默认参数

| 参数 | 默认值 |
|---|---|
| `TenkanPeriod` | 9 |
| `KijunPeriod` | 26 |
| `SenkouSpanBPeriod` | 52 |
| `HurstPeriod` | 100 个收盘价 |
| `HurstThreshold` | 0.5 |
| `SignalCooldownBars` | 6 |
| `AtrPeriod` | 14 |
| `StopLossAtr` | 2 |
| `CandleType` | 15 分钟 |

测试在附带的 BTC/TON 历史数据上，用独立计算的云线和收益率 R/S 核对每笔信号委托及成交；另行验证赫斯特预热、固定 ATR 距离以及蜡烛内部的真实保护成交。

历史数据中的保护对比采用 Tenkan/Kijun/Senkou B 15/40/70 和 HurstThreshold 0.45，以实际触发默认的 2 × ATR 止损距离；默认 9/26/52 信号另行核对。保护测试的开仓量为 3。

