# ColorJFatl StDev 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MQL5の **ColorJFatl_StDev** エキスパートアドバイザーをStockSharp APIに移植したものです。Jurik移動平均（JMA）と標準偏差バンドを組み合わせてトレードシグナルを生成します。

## 戦略ロジック

1. 終値でJMAを計算します。
2. 設定可能な期間にわたって標準偏差を算出します。
3. 乗数 `K1` と `K2` を使って2組の動的バンドを構築します：
   - `upper1 = JMA + K1 * StdDev`
   - `upper2 = JMA + K2 * StdDev`
   - `lower1 = JMA - K1 * StdDev`
   - `lower2 = JMA - K2 * StdDev`
4. 選択したシグナルモードに応じて、戦略はポジションを開閉します：
   - **Point** – 価格がバンドを越えた時にトリガー。
   - **Direct** – JMAラインの転換点を使用。
   - **Without** – 対応するシグナルを無効化。

## パラメーター

| 名前 | 説明 |
|------|------|
| `CandleTimeFrame` | ローソク足データの時間軸。 |
| `JmaLength` | Jurik移動平均の期間。 |
| `JmaPhase` | JMA計算のフェーズ。 |
| `StdPeriod` | 標準偏差の期間。 |
| `K1` | 最初の偏差乗数。 |
| `K2` | 2番目の偏差乗数。 |
| `BuyOpenMode` | ロングポジション開設モード。 |
| `SellOpenMode` | ショートポジション開設モード。 |
| `BuyCloseMode` | ロングポジション決済モード。 |
| `SellCloseMode` | ショートポジション決済モード。 |

## 使用方法

戦略は指定された時間軸のローソク足を購読し、JMAと標準偏差の値を処理して、定義されたモードに基づいて自動的に成行注文を送信します。

この実装は明確さを重視しており、さらなる機能強化やカスタムリスク管理のための出発点として機能します。
