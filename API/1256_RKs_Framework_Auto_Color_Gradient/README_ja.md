# RK's Framework 自動カラーグラジエント戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bollinger Bands %B と RSI を単一のオシレーターに統合し、カラーグラジェントにマッピングして中心線を越えたときに取引します。

## ロジック
- Bollinger Bands %B と相対力指数を計算します。
- 両方を確率的プロセスで正規化して平均を求めます。
- 結果を選択可能なカラーグラジェントに変換します。
- 平均値がゼロを上回ったときに買います。
- 平均値がゼロを下回ったときに売ります。
