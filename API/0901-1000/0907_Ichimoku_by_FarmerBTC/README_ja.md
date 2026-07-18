# Ichimoku by FarmerBTC 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Ichimoku by FarmerBTC は、価格がIchimokuクラウドの上で推移し、クラウドが強気で、上位時間軸のSMAが上昇トレンドを確認し、出来高が移動平均に係数を掛けた値を上回るときにロングポジションを建てます。価格がクラウドを下回ると手仕舞います。

## 詳細

- **エントリー条件**: インジケーターシグナル
- **ロング/ショート**: ロングのみ
- **エグジット条件**: 反対のシグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `TenkanPeriod` = 10
  - `KijunPeriod` = 30
  - `SenkouSpanBPeriod` = 53
  - `SmaLength` = 13
  - `VolumeLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CandleType` = 1 hour
  - `HtfCandleType` = 1 day
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: Ichimoku, SMA, 出来高
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
