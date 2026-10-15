# SmartAssTrade V2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SmartAssTrade V2戦略は、複数の時間軸（1m、5m、15m、30m、60m）にわたるMACDヒストグラムと20期間移動平均線を、Williams %RおよびRSIフィルターと組み合わせてトレンドモメンタムを捉えます。オプションのトレーリングストップが利益を保護します。

## 詳細

- **エントリー条件**: 多数の時間軸でMACDヒストグラムとMAが上昇し、WPR/RSIの確認がある
- **ロング/ショート**: 両方
- **エグジット条件**: 価格がテイクプロフィットまたはストップロスに到達；オプションのトレーリングストップ
- **ストップ**: 絶対ストップロスとテイクプロフィット、オプションのトレーリング付き
- **デフォルト値**:
  - `Volume` = 1
  - `TakeProfit` = 35
  - `StopLoss` = 62
  - `UseTrailingStop` = false
  - `TrailingStop` = 30
  - `TrailingStopStep` = 1
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD, SMA, Williams %R, RSI
  - ストップ: オプション
  - 複雑さ: 中級
  - 時間軸: マルチ時間軸 (1m,5m,15m,30m,60m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
