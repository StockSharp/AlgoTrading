# 2Mars OKX 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は移動平均クロスオーバーと SuperTrend フィルターを組み合わせています。Bollinger Bands が利益目標を提供し、ATR ベースのストップロスがリスクを制限します。

## ルール
- **ロング**: シグナル EMA がベース EMA を上抜け、かつ価格が SuperTrend の上方にある。
- **ショート**: シグナル EMA がベース EMA を下抜け、かつ価格が SuperTrend の下方にある。
- **エグジット**: Bollinger の上限または下限バンドでの利益確定、またはATRに係数を乗じたストップロス。

## インジケーター
- EMA
- SuperTrend
- Bollinger Bands
- Average True Range
