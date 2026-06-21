# トレンドフォロー戦略 MM3 高値・安値
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高値と安値の 3 期間単純移動平均を使用します。価格が高値の SMA を上回って終値をつけるとロングポジションを開き、安値の SMA を下回ると決済します。

## 詳細

- **エントリー条件**: Close > SMA(high)。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: Close < SMA(low)。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: SMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
