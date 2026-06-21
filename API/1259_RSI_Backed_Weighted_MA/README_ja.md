# RSI & 逆加重 MA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は相対力指数と逆加重移動平均を変化率フィルターとともに使用します。RSI が閾値を超え MA の ROC が設定水準を下回るとロングポジションを開き、逆の条件でショートポジションを開きます。ATR ベースのトレーリングストップと固定比率のポジションサイジングを適用します。

## 詳細

- **エントリー条件**:
  - **ロング**: `RSI >= RsiLongSignal` かつ `MA ROC <= RocMaLongSignal`
  - **ショート**: `RSI <= RsiShortSignal` かつ `MA ROC >= RocMaShortSignal`
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナル、ストップロスまたはトレーリングストップ。
- **ストップ**: はい、ATR トレーリングストップと最大損失率。
- **デフォルト値**:
  - `RsiLength` = 20
  - `MaType` = RWMA
  - `MaLength` = 19
  - `RsiLongSignal` = 60
  - `RsiShortSignal` = 40
  - `TakeProfitActivation` = 5
  - `TrailingPercent` = 3
  - `MaxLossPercent` = 10
  - `FixedRatio` = 400
  - `IncreasingOrderAmount` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: RSI, Moving Average, ATR
  - ストップ: はい
  - 複雑さ: 高
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
