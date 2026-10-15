# BB RSI トレーリングストップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bollinger BandsとRSIモメンタムを組み合わせ、条件付きトレーリングストップでトレードを保護します。
価格が下限バンドを突破しRSIが売られすぎゾーンを脱したときにロングエントリー。上限バンドでRSI過買いのときにショートが発動します。

ストップロスは固定距離で始まり、価格が事前設定のオフセット分有利に動いた後にトレーリングストップへ切り替わります。

## 詳細

- **エントリー条件**: RSI確認を伴うBollinger Bandブレイクアウト
- **ロング/ショート**: 両方
- **エグジット条件**: 初期ストップロスまたはトレーリングストップ
- **ストップ**: あり、動的トレーリング
- **デフォルト値**:
  - `BollingerPeriod` = 25
  - `BollingerDeviation` = 2
  - `RsiPeriod` = 14
  - `RsiOverbought` = 60
  - `RsiOversold` = 33
  - `StopLossPoints` = 50
  - `TrailOffsetPoints` = 99
  - `TrailStopPoints` = 40
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Bollinger Bands, RSI
  - ストップ: トレーリング
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
