# CHO With Flat戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は**Chaikin Oscillator**とその移動平均線のクロスオーバーに基づいて取引します。ボリンジャーバンドフィルターを使用して、横ばい相場での取引を避けます。

## パラメーター
- **Candle Type** – 入力ローソク足の時間軸。
- **Fast Period** – Chaikin Oscillatorの速い期間。
- **Slow Period** – Chaikin Oscillatorの遅い期間。
- **MA Period** – オシレーターに適用する移動平均の期間。
- **MA Type** – シグナルラインの移動平均の種類。
- **Bollinger Period** – ボリンジャーバンドの期間。
- **Std Deviation** – ボリンジャーバンドの標準偏差。
- **Flat Threshold** – 市場がアクティブとみなされる最小バンド幅（ポイント）。

## 取引ロジック
1. Chaikin Oscillatorとその移動平均を計算する。
2. 横ばい相場の検出のために価格にボリンジャーバンドを構築する。
3. ボリンジャーバンドの幅が`Flat Threshold`を下回る場合は取引をスキップする。
4. オシレーターがシグナルラインを下に抜けたとき**買い**。
5. オシレーターがシグナルラインを上に抜けたとき**売り**。

ポジションの方向は常に最新のクロスオーバーに従い、フラットフィルターが横ばい相場での取引を防ぎます。
