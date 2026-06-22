# フラクタル加重オシレーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、4つのオシレーター（RSI、Money Flow Index、Williams %R、DeMarker）を単一の平滑化された複合シグナルに集約することで「Exp_Fractal_WeightOscillator」エキスパートアドバイザーを再現します。オシレーターは2つの水平レベル（`HighLevel`/`LowLevel`）と比較され、トレンドフォローまたはカウンタートレンドモードでロングまたはショート取引を発動します。すべての計算は選択されたローソク足時間軸で実行され、標準のStockSharp高レベルAPIを使用します。

## インジケータースタック
- **相対力指数（RSI）** – 設定された価格ソースに適用。
- **Money Flow Index** – 選択された適用価格とローソク足ボリュームから計算。
- **Williams %R** – ローソク足の高値/安値/終値から計算。
- **DeMarker** – 単純平均スムーザーを使用してローソク足の高値と安値から再構成。
- **移動平均スムーザー** – 加重合計のオプションの後処理（SMA、EMA、SMMA、またはLWMA）。

複合オシレーター値は4つのコンポーネントの加重平均です。`HighLevel`と`LowLevel`は買われすぎ/売られすぎゾーンを定義します。`SignalBar`はクロスオーバーを探す際に検査される完成したバーの数を制御し、最新の完成したローソク足に対して実行を遅延させることができます。

## トレードロジック
### TrendMode = Direct
- **ロングエントリー / ショートエグジット** – オシレーターが`LowLevel`より上から`LowLevel`以下に下落したとき（`BuyOpenEnabled`と`SellCloseEnabled`がtrueでなければならない）。
- **ショートエントリー / ロングエグジット** – オシレーターが`HighLevel`より下から`HighLevel`以上に上昇したとき（`SellOpenEnabled`と`BuyCloseEnabled`がtrueでなければならない）。

### TrendMode = Counter
- **ロングエントリー / ショートエグジット** – `HighLevel`の上方ブレイクによって発動。
- **ショートエントリー / ロングエグジット** – `LowLevel`の下方ブレイクによって発動。

シグナルは`SignalBar`で指定されたバーで評価されます。ポジション反転は既存のエクスポージャーを無効にするために`ボリューム + |ポジション|`を使用します。

## リスク管理
新しいポジションが開かれると、戦略は`StopLossPoints`と`TakeProfitPoints`を使用して固定価格のストップロスとテイクプロフィットレベルを計算します。値はインストゥルメントの`MinPriceStep`で乗算されます。完成した各ローソク足で安値/高値がこれらのターゲットに対してチェックされます。ヒットした場合、ポジションはすぐに閉じられ、内部リスクトラッカーがリセットされます。

## パラメーター
| 名前 | 説明 |
| ---- | ---- |
| `TrendMode` | ダイレクト（トレンドフォロー）またはカウンタートレンド動作を選択。 |
| `SignalBar` | シグナル評価に使用される閉じたバーの数（後ろ向き）。 |
| `Period` | RSI、MFI、Williams %R、DeMarkerのベース長。 |
| `SmoothingLength` | 移動平均スムーザーのウィンドウ。 |
| `SmoothingMethod` | 移動平均のタイプ（`None`、`Sma`、`Ema`、`Smma`、`Lwma`）。 |
| `RsiPrice`、`MfiPrice` | コンポーネントオシレーターで使用される適用価格ソース。 |
| `MfiVolume` | MFIのボリュームタイプ（tickとrealの両方がローソク足ボリュームを使用）。 |
| `RsiWeight`、`MfiWeight`、`WprWeight`、`DeMarkerWeight` | 複合オシレーターでの相対的な重み。 |
| `HighLevel`、`LowLevel` | レベルクロスのための上限と下限のしきい値。 |
| `BuyOpenEnabled`、`SellOpenEnabled` | ロングまたはショートエントリーを有効化。 |
| `BuyCloseEnabled`、`SellCloseEnabled` | 反対のシグナルで既存のポジションをクローズすることを許可。 |
| `StopLossPoints`、`TakeProfitPoints` | 価格ステップでの保護距離（0でレベルを無効化）。 |
| `CandleType` | 戦略に渡されるローソク足の時間軸。 |
| `Volume` *(戦略プロパティ)* | エントリーに使用する取引サイズ（ポジション反転は絶対ポジションを追加）。 |

## 使用上の注意
- `SignalBar = 1`は最後に完全に閉じたバーを使用することでオリジナルエキスパートの動作を再現します。値を増やすと、追加のバーだけ反応が遅延します。
- `SmoothingMethod`は平滑化をオフにする（`None`）か、MQLバージョンで利用可能な異なる移動平均スタイルに一致させることができます。
- Money Flow Index実装は常にデータフィードから供給されるローソク足の合計ボリュームで動作します。StockSharpのローソク足はデフォルトで個別のティックカウンターを公開しないため、`Tick`と`Real`の両オプションは同じ集約値を指します。
- C#ソース内のすべてのコメントは必要に応じて英語で書かれています。
