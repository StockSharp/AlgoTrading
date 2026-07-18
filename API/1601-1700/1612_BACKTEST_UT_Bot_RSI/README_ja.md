# バックテスト UT Bot + RSI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

UT Botトレンド検出器とRSIレベルを組み合わせます。RSIが売られすぎの時にUT Botが上昇反転すればロングエントリー、RSIが買われすぎの時に下落反転すればショートエントリーします。

## 詳細

- **エントリー条件**:
  - **ロング**: UT Botが上向きに転換し、RSI < `RSI Oversold`。
  - **ショート**: UT Botが下向きに転換し、RSI > `RSI Overbought`。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - テイクプロフィットまたはストップロスのパーセンテージ。
- **ストップ**: テイクプロフィットとストップロス。
- **デフォルト値**:
  - `RSI Length` = 14
  - `RSI Overbought` = 60
  - `RSI Oversold` = 40
  - `ATR Length` = 10
  - `UT Bot Factor` = 1.0
  - `Take Profit %` = 3.0
  - `Stop Loss %` = 1.5
- **フィルター**:
  - カテゴリ: Trend Following
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
