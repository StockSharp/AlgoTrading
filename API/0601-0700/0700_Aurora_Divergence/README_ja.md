# Aurora ダイバージェンス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は価格とOn-Balance Volume (OBV)の間のダイバージェンスを取引します。価格とOBVの線形回帰の傾きを比較して潜在的なリバーサルを検出します。

## 主な特徴

- ダイバージェンスシグナルのための線形回帰傾き比較。
- 過度に伸びた価格を避けるためのオプションのz-scoreフィルター。
- トレンド確認のための上位時間軸移動平均フィルター。
- ATRベースのボラティリティ閾値と動的なストップ・目標によるリスク管理。
- 各取引後のクールダウンとポジション内の最大バー数。

## パラメーター

| 名前 | 説明 |
|------|------|
| `CandleType` | メイン計算のローソク足時間軸。 |
| `Lookback` | 傾き計算の期間。 |
| `ZLength` | z-scoreフィルターの平均と標準偏差のルックバック。 |
| `ZThreshold` | エントリーを許可する最大絶対z-score。 |
| `UseZFilter` | z-scoreフィルターの有効/無効。 |
| `HtfCandleType` | トレンド移動平均の上位時間軸。 |
| `HtfMaLength` | 上位時間軸の移動平均の長さ。 |
| `AtrLength` | ボラティリティとリスクのATR期間。 |
| `AtrThreshold` | 取引を許可する最小ATR値。 |
| `StopAtrMultiplier` | ストップロス距離のATR乗数。 |
| `ProfitAtrMultiplier` | テイクプロフィット距離のATR乗数。 |
| `MaxBarsInTrade` | ポジションを保有する最大バー数。 |
| `CooldownBars` | 取引後に次のシグナルを出すまでの待機バー数。 |

## 複雑さ

中級
