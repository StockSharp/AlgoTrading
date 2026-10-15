# Ichimoku クラウド買いカスタムEMAエグジット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

カスタムEMAエグジットと出来高フィルターを備えたIchimokuクラウド買い戦略の実装です。価格がクラウドの上にあり、出来高が平均を超えているときに買いエントリーします。オプションで価格がEMAの上にあることを要求できます。価格がEMAを下回るか、またはストップロスに達した時点でポジションを手仕舞います。

## 詳細

- **エントリー条件**:
  - ロング: `Price > Cloud && Volume > AvgVolume && (Price > EMA if enabled)`
- **ロング/ショート**: ロングのみ
- **エグジット条件**:
  - `Price < EMA`
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `EmaLength` = 44
  - `VolumeAvgPeriod` = 10
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: Ichimoku Cloud, EMA, 出来高
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
