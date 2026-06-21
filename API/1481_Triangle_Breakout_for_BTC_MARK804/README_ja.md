# BTC向けトライアングル・ブレイクアウト戦略 (MARK804)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

出来高が急増した際のSMAトライアングルのブレイクアウトをトレードし、ATRベースのストップでポジションを管理します。

## 詳細

- **エントリー条件**: 出来高がSMAを上回る状態で、価格が上側SMAラインを上抜けまたは下側SMAラインを下抜ける
- **ロング/ショート**: 両方
- **エグジット条件**: ATRベースのストップロスまたはテイクプロフィット
- **ストップ**: あり
- **デフォルト値**:
  - `TriangleLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrLength` = 14
  - `VolumeMultiplier` = 1.5
  - `AtrMultiplierSl` = 1.0
  - `AtrMultiplierTp` = 1.5
  - `CandleType` = 1時間
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: SMA, ATR, 出来高
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
