# VWAP 標準偏差バンド戦略（ロングのみ）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がVWAP標準偏差の下限バンドを下抜けたときに買いを入れ、利益目標に達したらポジションを閉じます。

## パラメーター

- **DevUp**: VWAPより上の標準偏差乗数。
- **DevDown**: VWAPより下の標準偏差乗数。
- **ProfitTarget**: 価格単位での利益目標。
- **GapMinutes**: 新規注文前の待機時間（分）。
- **CandleType**: ローソク足の種類。
