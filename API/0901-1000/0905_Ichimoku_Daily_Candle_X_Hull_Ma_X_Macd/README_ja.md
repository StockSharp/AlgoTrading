# Ichimoku Daily Candle X Hull MA X MACD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Ichimokuの先行線、日足ローソク足の方向、Hull Moving Averageのトレンド、およびHMAベースのMACDを組み合わせています。すべてのコンポーネントが強気に揃ったときにロングポジションを建て、すべての条件が弱気に転じたときにショートを行います。

## 詳細

- **エントリー条件**:
  - **ロング**: HMAが上昇、現在の価格が前のHMAを上回る、現在の日足ローソク足が前より高い、SenkouA > SenkouB、MACDライン > シグナル。
  - **ショート**: HMAが下落、価格が前のHMAを下回る、現在の日足ローソク足が前より低い、SenkouA < SenkouB、MACDライン < シグナル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `HmaPeriod` = 14
  - `ConversionPeriod` = 9
  - `BasePeriod` = 26
  - `SpanPeriod` = 52
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `PriceSource` = Open
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Ichimoku, Hull MA, MACD
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
