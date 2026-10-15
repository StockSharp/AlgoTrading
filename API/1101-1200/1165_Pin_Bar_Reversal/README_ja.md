# Pin Barリバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

トレンドフィルターとATRベースのストップ・ターゲットを組み合わせたPin Barローソク足を使用します。SMAの上方にある強気のPin Barはロングポジションを開き、SMAの下方にある弱気のPin Barはショートポジションを開きます。ボラティリティが低すぎる場合はエントリーをスキップします。

## 詳細

- **エントリー条件**: トレンド方向のPin Bar（長いヒゲ、小さいボディ、ATRが`MinAtr`を上回る）。
- **ロング/ショート**: 両方。
- **エグジット条件**: ATRベースのストップロスまたはテイクプロフィット。
- **ストップ**: はい、ATRの倍数。
- **デフォルト値**:
  - `TrendLength` = 50
  - `MaxBodyPct` = 0.30
  - `MinWickPct` = 0.66
  - `AtrLength` = 14
  - `StopMultiplier` = 1
  - `TakeMultiplier` = 1.5
  - `MinAtr` = 0.0015
  - `CandleType` = 1 hour
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 両方
  - インジケーター: SMA, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
