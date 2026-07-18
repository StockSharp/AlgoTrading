# Color Zerolag RVI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は相対活力指数（RVI）とそのシグナルラインを使用します。
RVIのメインラインがシグナルラインを下向きに突き抜けたときに買いエントリーし、メインラインがシグナルラインを上向きに突き抜けたときに売りエントリーします。

## 詳細

- **エントリー条件**: RVIとシグナルラインのクロス
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のシグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `RviLength` = 14
  - `SignalLength` = 9
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = 4時間
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RVI, SMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (H4)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
