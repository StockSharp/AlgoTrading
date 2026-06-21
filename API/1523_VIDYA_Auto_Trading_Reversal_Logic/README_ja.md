# VIDYA 自動トレーディング（リバーサルロジック）戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、広いATRバンドを持つVariable Index Dynamic Average（VIDYA）を適用します。
価格が上限バンドを上抜けたときにロングトレードを建て、価格が下限バンドを下抜けたときにショートトレードを建てます。

## 詳細

- **エントリー条件**: 価格がVIDYA周辺のATRバンドをクロス
- **ロング/ショート**: 両方
- **エグジット条件**: 反対方向のバンドブレイクアウト
- **ストップ**: なし
- **デフォルト値**:
  - `VidyaLength` = 10
  - `VidyaMomentum` = 20
  - `BandDistance` = 2
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: VIDYA, ATR
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
