# AUD/USD スキャルピング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAトレンドフィルター、Bollinger Bands、RSIを組み合わせて短い時間軸でAUD/USDをスキャルピングする戦略。高速・低速のEMAがトレンド方向を定義します。上昇トレンド時に価格がBollinger Bandの下部バンドに触れRSIが売られすぎ閾値を上回った場合にロングエントリーします。下降トレンド時に価格が上部バンドに達しRSIが買われすぎレベルを下回った場合にショートエントリーします。固定のテイクプロフィットとストップロスでリスクを管理します。

## 詳細

- **エントリー条件**:
  - **ロング**: 高速EMAが低速EMAを上回り、価格がBollinger Bandの下部バンド以下、RSIが売られすぎレベルを上回る。
  - **ショート**: 高速EMAが低速EMAを下回り、価格がBollinger Bandの上部バンド以上、RSIが買われすぎレベルを下回る。
- **ロング/ショート**: 両サイド。
- **エグジット条件**: ストップロスまたはテイクプロフィット。
- **ストップ**: 固定ストップロスとテイクプロフィット。
- **デフォルト値**:
  - `EmaShort` = 13
  - `EmaLong` = 26
  - `RsiPeriod` = 4
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `TakeProfit` = 0.0005
  - `StopLoss` = 0.0004
- **フィルター**:
  - カテゴリ: スキャルピング
  - 方向: 両方
  - インジケーター: EMA, Bollinger Bands, RSI
  - ストップ: 固定
  - 複雑さ: 低
  - 時間軸: 1分
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
