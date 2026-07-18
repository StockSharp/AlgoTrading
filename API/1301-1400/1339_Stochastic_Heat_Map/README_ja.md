# Stochasticヒートマップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Stochastic Heat Map戦略は、期間を増やしながら複数のStochasticオシレーターの平均を取ります。
組み合わせた値を再度平滑化して、速い線と遅い線を形成します。
速い線が遅い線を上抜けたときにロング、逆のクロスでショートします。

## 詳細

- **エントリー条件**: 速い線/遅い線のクロス
- **ロング/ショート**: 両方
- **エグジット条件**: 逆のシグナル
- **ストップ**: なし
- **デフォルト値**:
  - `CandleType` = 15 minute
  - `Increment` = 10
  - `SmoothFast` = 2
  - `SmoothSlow` = 21
  - `PlotNumber` = 28
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Stochastic
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
