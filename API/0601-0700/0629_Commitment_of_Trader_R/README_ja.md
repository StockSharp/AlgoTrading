# Commitment of Trader R 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は Williams %R インジケーターを使用して買われすぎ・売られすぎの状態を検出します。単純移動平均がオプションのトレンドフィルターとして機能します。

Williams %R が上限閾値を上回り、終値が SMA を上回ったときにロングを建てます。Williams %R が下限閾値を下回り、価格が SMA を下回ったときにショートを建てます。オシレーターがシグナルゾーンを離れたときにポジションをクローズします。

## 詳細
- **エントリー条件**:
  - **ロング**: %R > 上限閾値 かつ（有効時は価格 > SMA）
  - **ショート**: %R < 下限閾値 かつ（有効時は価格 < SMA）
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: %R < 上限閾値
  - **ショート**: %R > 下限閾値
- **ストップ**: いいえ
- **デフォルト値**:
  - `WilliamsPeriod` = 252
  - `UpperThreshold` = -10
  - `LowerThreshold` = -90
  - `SmaEnabled` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: Williams %R, SMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
