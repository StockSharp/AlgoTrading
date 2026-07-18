# Bbsr Extreme 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Bbsr Extreme**戦略は、Bollinger Bandsのブレイクアウトと移動平均に基づくトレンドフィルターを組み合わせます。
平均が上昇中に価格が下限バンドから反発したときにロングポジションが発生します。
平均が下落中に価格が上限バンドから引き戻したときにショートポジションが開かれます。
エグジットはATRベースのストップロスとテイクプロフィットに依存します。

## 詳細
- **エントリー条件**: トレンド確認を伴う価格のバンドクロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATRストップまたはテイクプロフィット。
- **ストップ**: あり、ATRベース。
- **デフォルト値**:
  - `BollingerPeriod = 20`
  - `BollingerMultiplier = 2`
  - `MaLength = 7`
  - `AtrLength = 14`
  - `AtrStopMultiplier = 2`
  - `AtrProfitMultiplier = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Bollinger Bands, EMA, ATR
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
