# Swing FX Pro Panel v1 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

基本的なパフォーマンス統計を備えたEMAクロスオーバーを使用するデモンストレーション戦略。速いEMAが遅いEMAを上向きにクロスするとロングポジションを開き、下向きにクロスするとショートポジションを開きます。各取引には固定の利益と損失の目標が使用されます。

## 詳細

- **インジケーター**: EMA
- **パラメーター**:
  - `Initial Capital` – 統計用の初期口座サイズ。
  - `Risk Per Trade` – 取引あたりのリスクのパーセンテージ（参考値）。
  - `Analysis Period` – 分析に使用する期間の長さ。
  - `Fast Length` – 速いEMAの期間。
  - `Slow Length` – 遅いEMAの期間。
  - `Profit Target` – 価格単位での利益。
  - `Stop Loss` – 価格単位での損失。
