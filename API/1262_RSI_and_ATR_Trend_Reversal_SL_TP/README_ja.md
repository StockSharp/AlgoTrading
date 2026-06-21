# RSI と ATR トレンドリバーサル SL TP 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSI と ATR を使ってトレンドのリバーサルを検出し、動的なストップロスおよびテイクプロフィットレベルを設定する戦略です。

## 詳細

- **エントリー条件**: 価格が適応的 RSI/ATR 閾値を越える。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆方向のクロス。
- **ストップ**: 動的閾値を通じて統合。
- **デフォルト値**:
  - `RsiLength` = 8
  - `RsiMultiplier` = 1.5
  - `Lookback` = 1
  - `MinDifference` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: RSI, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
