# トリプルEMAクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

3本の単純移動平均線に基づく戦略。
短期SMAが中期SMAを上抜けし、3本すべてが上向きに整列したときにロングポジションを建てる。
逆のクロスオーバーと整列でショートポジションを建てる。
価格が短期SMAを再び下抜けするとポジションを決済する。

## 詳細

- **エントリー条件**: トレンドフィルターを使用したSMA1とSMA2のクロスオーバー。
- **ロング/ショート**: 両方。
- **エグジット条件**: 価格がSMA1をクロスするか保護的ストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `Sma1Period` = 9
  - `Sma2Period` = 21
  - `Sma3Period` = 55
  - `StopLossTicks` = 200
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: 固定
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: なし
  - リスクレベル: 中
