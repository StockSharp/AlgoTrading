# Demo GPT - 日内剥头皮策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

当连续两根阳线出现且第二根的开盘价高于第一根的收盘价，并且成交量高于20柱平均值时建立多头仓位。当连续两根阴线出现且第二根的开盘价低于第一根的收盘价，并且成交量同样高于平均值时平仓。额外计算EMA(20)、VWAP和Camarilla水平供参考。

### 实际条件与默认参数

默认：`EmaLength=20`、`VolumeLookback=20`、`CandleType=1分钟`。收集至少**20根先前已完成K线**后，成交量条件为 `当前 TotalVolume > 先前20根的平均 TotalVolume`（当前K线不参与均值）。仅空仓时、前后两根均阳线（`Close > Open`）、**当前 `Open > 前一根 Close`** 且放量，才**做多**。前后两根均阴线（`Close < Open`）、当前 `Open < 前一根 Close` 且放量时，**全部平掉实际多仓**。若没有严格的跳空开盘或放量，则**不是信号**；从不做空。EMA20 为收盘价指标；日内VWAP按 `sum(典型价 × 成交量)/sum(成交量)` 每日重置；前一日Camarilla `H3/L3 = 前日收盘 ± 1.1 × (前日最高 − 前日最低) / 4`。这些仅供参考，不参与入场过滤。无额外止损。

