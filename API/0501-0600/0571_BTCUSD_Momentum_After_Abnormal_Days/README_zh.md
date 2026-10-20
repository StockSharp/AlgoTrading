# BTCUSD Momentum After Abnormal Days

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

已完成日线的收益率为 `r=(Close-Open)/Open`。使用之前 `LookbackPeriod` 个已完成日收益计算均值和总体标准差，排除当前收益，且要求前窗完整。`r>mean+k*sd` 时买入，`r<mean-k*sd` 时卖出，`k=AbnormalThreshold`。等于阈值不入场。

仅空仓入场。数量为 `floor(CapitalPerTrade/Close/VolumeStep)*VolumeStep`；不足最小量则跳过，超过最大量则拒绝。资金按信号收盘价计量，实际市价成交可能不同。下一根日线收盘时平掉全部已成交仓位；缺失数据时在首个更晚的可用日线收盘退出。该足优先退出，不同时重新入场。未完成订单防止重复发单，无价格止损。

## 参数

| 参数 | 默认值 |
|---|---|
| `CandleType` | `1d` |
| `LookbackPeriod` | `5` |
| `AbnormalThreshold` | `1.6` |
| `CapitalPerTrade` | `1000` |
