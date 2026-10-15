# 数理統計カーネル関数戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

複数の統計カーネル関数を示し、選択したカーネルの出力が0.5を越えたときにトレードを行います。

## パラメーター
- **Kernel** – カーネル関数名（`uniform`, `triangle`, `epanechnikov`, `quartic`, `triweight`, `tricubic`, `gaussian`, `cosine`, `logistic`, `sigmoid`）。
- **Bandwidth** – カーネルの帯域幅。
- **Candle Type** – ローソク足の時間軸。
