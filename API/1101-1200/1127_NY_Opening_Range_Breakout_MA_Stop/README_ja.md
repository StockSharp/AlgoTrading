# NY オープニングレンジブレイクアウト - MAストップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ニューヨーク9:30-9:45のオープニングレンジのブレイクアウトを、オプションの移動平均ベースの決済で取引する戦略。カットオフ時刻内でMAフィルターを満たす場合、ブレイクアウト後の次のバーでエントリーする。

## 詳細

- **エントリー条件**:
  - 前のローソク足がカットオフ時刻前にオープニングレンジの高値（ロング）または安値（ショート）を超えて終値をつける。
  - 現在のローソク足がブレイクアウト後最初のものであり、有効な場合にMAフィルターを満たす。
- **ロング/ショート**: `TradeDirection`で設定可能。
- **エグジット条件**:
  - オープニングレンジの反対側にストップ。
  - `TakeProfitType`に従ったテイクプロフィット: 固定リスクリワード、移動平均クロス、または両方。
- **ストップ**: はい、レンジ境界に設定。
- **デフォルト値**:
  - `CutoffHour` = 12
  - `CutoffMinute` = 0
  - `TradeDirection` = LongOnly
  - `TakeProfitType` = FixedRiskReward
  - `TpRatio` = 2.5
  - `MaType` = SMA
  - `MaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 設定可能
  - インジケーター: Moving Average
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
