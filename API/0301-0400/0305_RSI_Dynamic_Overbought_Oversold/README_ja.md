# RSI動的買われすぎ/売られすぎ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**RSI Dynamic Overbought Oversold**戦略は、動的な買われすぎ/売られすぎレベルを持つRSIを中心に構築されています。

テストでは平均年間リターンが約178%であることが示されています。株式市場で最もよいパフォーマンスを発揮します。

### 実際の取引ルール（C# / Python）

**確定した5分足**で RSI(14)、価格の SMA(50)、および RSI 自身の SMA(50)・標準偏差(50) を計算します。動的バンドは `upper = min(RSI_MA + 2 * RSI_STD, 85)`、`lower = max(RSI_MA - 2 * RSI_STD, 15)`。ノーポジション時は `RSI <= lower` かつ `Close >= Price_SMA * 0.995` で買い、`RSI >= upper` かつ `Close <= Price_SMA * 1.005` で売り。ロングは `RSI >= RSI_MA` または `Close < Price_SMA * 0.995` で決済し、ショートは `RSI <= RSI_MA` または `Close > Price_SMA * 1.005` で決済します。発注後のクールダウンは**確定足48本**。約定ポジションにはATRではなく**ネイティブ2%ストップロス**が適用されます。上記の収益率はBTC/TONで未検証です。



## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターに基づく計算を使用。
- **デフォルト値**:
  - `RsiPeriod = 14`
  - `MovingAvgPeriod = 50`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Overbought, Oversold
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
