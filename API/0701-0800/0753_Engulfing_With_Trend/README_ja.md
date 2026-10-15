# トレンドに沿ったエングルフィング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、SuperTrendフィルターと強気・弱気のエングルフィングパターンを組み合わせます。現在のトレンド方向に前のバーを飲み込むローソク足が出現したときに取引を開始します。ストップとターゲットのレベルはパターンの値幅から計算されます。

## 詳細

- **エントリー条件**: SuperTrendの方向に沿ったエングルフィングパターン。
- **ロング/ショート**: 両方。
- **エグジット条件**: ストップロスまたはテイクプロフィット。
- **ストップ**: あり、ローソク足の極値とATRオフセットに基づく。
- **デフォルト値**:
  - `CandleType` = 5分
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3
  - `BoringThreshold` = 25
  - `EngulfingThreshold` = 50
  - `StopLevel` = 200
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 両方
  - インジケーター: SuperTrend, Candlestick
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
