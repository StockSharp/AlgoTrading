# Bollinger Bands 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bollinger Bandsのブレイクアウトを取引する戦略です。価格が上限バンドを上回って終値が付いたときに買い、下限バンドを下回って終値が付いたときに売ります。単純移動平均クロスまたはストップロス到達時に決済します。

## 詳細

- **エントリー条件**:
  - ロング: Bollinger Bands上限を上回る終値
  - ショート: Bollinger Bands下限を下回る終値
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: SMAを下回る終値またはストップロス到達
  - ショート: SMAを上回る終値またはストップロス到達
- **ストップ**: エントリー価格からのパーセント
- **デフォルト値**:
  - `BbLength` = 120
  - `BbDeviation` = 2m
  - `SmaLength` = 110
  - `StopLossPercent` = 6m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Bollinger Bands, SMA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
