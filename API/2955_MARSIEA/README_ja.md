# MA RSI EA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**MA RSI EA戦略**は、高速移動平均線と短期RSIフィルターを組み合わせた元のMetaTraderエキスパートアドバイザーのロジックを再現します。戦略は選択したローソク足シリーズで取引し、確定済みバーのみで新しい注文を評価し、口座残高または純資産に基づく動的なポジションサイジングを使用します。すべてのオープンポジションの浮動利益が正になると、利益を確定するためにすべてのポジションが即座にクローズされます。

## インジケーター
- **Moving Average** – 価格ソース選択とオプションのシフトを持つ設定可能なメソッド（シンプル、指数、スムーズド、線形加重）。
- **Relative Strength Index (RSI)** – MQLバージョンと同じローソク足価格ファミリーから読み取る短期オシレーター。

## トレードロジック
1. 各完了ローソク足について、戦略は設定された価格ソースを使用して移動平均線とRSIの値を計算します。
2. 最新の移動平均値は、MQL動作と一致させるためにユーザー定義のバー数だけシフトできます。
3. 現在の純ポジションの浮動PnLを評価します：
   - すべてのオープンポジションの浮動結果が**ゼロより大きい**場合、戦略は利益を実現するために全ポジションをクローズします。
   - 浮動結果が**負**の場合、損失の小さい側（買い側対売り側）をその方向に追加トレードを開いて強化します。
4. 平均化シグナルがない場合、RSI + MAフィルターが適用されます：
   - **ショートエントリー** – RSI ≥ `RsiOverbought`かつローソク足の始値がシフトした移動平均線を下回る。
   - **ロングエントリー** – RSI ≤ `RsiOversold`かつローソク足の始値がシフトした移動平均線を上回る。

## エグジットロジック
- 正の浮動PnLが`CloseAllPositions`をトリガーし、戦略を即座にフラット化します。
- 平均化ロジックからの手動反転シグナルは、StockSharpがネットポジションで動作するため反対側のエクスポージャーをクローズします。

## ポジションサイジング
`LotSizingModes`はEAの`OptLot`選択を反映します：
- **Fixed** – 常に`LotSize`ボリュームを送信します。
- **Balance** – ローソク足クローズ価格を使用して`PercentOfBalance`のポートフォリオ価値をボリュームに変換します。
- **Equity** – 現在のポートフォリオ純資産の`PercentOfEquity`をボリュームに変換します。

計算されたボリュームは最寄りの`Security.VolumeStep`に丸められ（利用可能な場合）、注文がインストルメントのロットサイズに準拠するようにします。

## パラメーター
| パラメーター | 説明 | デフォルト |
|------------|------|-----------|
| `LotOption` | ボリューム計算モード（`Fixed`、`Balance`、`Equity`）。 | `Balance` |
| `LotSize` | `Fixed`モードの固定ロット値。 | `0.01` |
| `PercentOfBalance` | `Balance`モードで使用する残高のパーセンテージ。 | `2` |
| `PercentOfEquity` | `Equity`モードで使用する純資産のパーセンテージ。 | `3` |
| `FastMaPeriod` | 移動平均線の長さ。 | `4` |
| `FastMaShift` | 移動平均結果に適用するシフト。 | `0` |
| `FastMaMethod` | 移動平均計算メソッド（`Simple`、`Exponential`、`Smoothed`、`LinearWeighted`）。 | `LinearWeighted` |
| `FastMaPrice` | 移動平均線のローソク足価格ソース。 | `Open` |
| `RsiPeriod` | RSIの長さ。 | `4` |
| `RsiPrice` | RSIのローソク足価格ソース。 | `Open` |
| `RsiOverbought` | 買われすぎ市場を定義するRSIレベル。 | `80` |
| `RsiOversold` | 売られすぎ市場を定義するRSIレベル。 | `20` |
| `CandleType` | 戦略が使用するローソク足シリーズ。 | `15分タイムフレーム` |

## ローソク足価格ソース
`CandlePriceSources`はMQLの適用価格リストを複製します：
- `Open`、`High`、`Low`、`Close`
- `Median` = (High + Low) / 2
- `Typical` = (High + Low + Close) / 3
- `Weighted` = (High + Low + Close + Close) / 4

## 注意事項
- 注文は戦略がオンラインで、ローソク足が完了したときのみ生成されます。新しいバーでトリガーする元のEAと一致します。
- StockSharpはネットポジションを維持するため、平均化シグナルはヘッジポジションを作成する代わりに現在のエクスポージャーを自動的に削減または反転します。
- Python実装は要求に従い意図的に省略されています。
