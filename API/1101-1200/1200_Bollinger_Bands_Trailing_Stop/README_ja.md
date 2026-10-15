# ボリンジャーバンドとトレーリングストップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がボリンジャーバンドの上限バンドを上回って終値をつけたときにロングエントリーします。
価格が下限バンドを下回って終値をつけるか、ATRベースのトレーリングストップが発動したときにイグジットします。

## 詳細

- **エントリー条件**: 上限バンドを上回るクローズ。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 下限バンドを下回るクローズ、またはトレーリングストップ発動。
- **ストップ**: トレーリング。
- **デフォルト値**:
  - `BbLength` = 20
  - `BbDeviation` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: Bollinger Bands, ATR
  - ストップ: あり
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
