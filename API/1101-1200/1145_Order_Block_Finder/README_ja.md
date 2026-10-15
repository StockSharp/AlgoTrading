# オーダーブロック検出戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、指定された数の連続したローソク足と最小パーセント移動に基づいて強気と弱気のオーダーブロックを識別します。強気のオーダーブロックが検出された場合は買い、弱気のブロックが見つかった場合は売りを行います。

## パラメーター
- **Relevant Periods** – オーダーブロックを確認するための後続ローソク足の数
- **Min Percent Move** – ブロックと最後の確認ローソク足の間の最小パーセント変化
- **Use Whole Range** – Open基準の境界の代わりにHigh/Lowレンジを使用
- **Candle Type** – 計算に使用するローソク足タイプ
