# P-Square 第N百分位戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

P-Squareアルゴリズムを使用して、ソース系列の選択した百分位を推定します。値が上位百分位を超えた場合にロングポジションを建て、下位百分位を下回った場合にショートポジションを建てます。

## パラメーター
- `Percentile` – 推定する百分位。
- `UseReturns` – 価格ではなくリターンを処理する。
- `CandleType` – ローソク足のデータ型。
