# SMAクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は2つの単純移動平均（SMA）を使用します：短期と長期のものです。

- 短期SMAが長期SMAを上から下へクロスしたときに買います。
- 短期SMAが長期SMAを上から下へクロスしたときに売ります。

## パラメーター
- **短期の長さ** – 短期SMAの期間。
- **長期の長さ** – 長期SMAの期間。
- **ローソク足タイプ** – 計算のための時間軸。
