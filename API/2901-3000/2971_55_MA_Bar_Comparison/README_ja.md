# FiftyFiveMaBarComparisonStrategy 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略はMetaTrader 5の「55 MA」エキスパートアドバイザーを再現したもので、55期間の移動平均線の2点を比較し、その差が設定可能な閾値を超えた場合に取引を行います。すべての計算はユーザー定義のイントラデイセッション内の完成したローソク足で実行され、取引方向はオプションで反転できます。アルゴリズムは、強気条件が満たされない場合は常にショートポジションが開かれるという元の動作を保持します。

## 取引ロジック
1. 選択したローソク足シリーズにサブスクライブし、選択した長さ、手法、適用価格で移動平均線を計算します。
2. 水平MAシフトが使用されている場合でも、バーインデックス `BarA` と `BarB` の値にアクセスできるよう、最新の移動平均値をバッファに保持します。
3. `[StartHour, EndHour)` ウィンドウ内に完成したローソク足が到着した場合:
   - `BarA + MaShift` と `BarB + MaShift` でのMA値を取得します。
   - `BarA` の値が `BarB` の値を `DifferenceThreshold` 以上上回る場合、`ReverseSignals` が有効でない限りロングポジションを開きます。
   - `BarA` の値が `BarB` の値を `DifferenceThreshold` 以上下回る場合、ショートポジションを開きます（`ReverseSignals` が有効な場合はロングポジション）。
   - それ以外の場合、戦略は元のEAの動作を維持し、ショートエントリーをトリガーします。
4. 注文は常に戦略の `Volume` を使用して成行で送信されます。`CloseOppositePositions` が有効な場合、新しいポジションを確立する前に反対のエクスポージャーを解消するためにリクエストサイズが増加されます。
5. オプションのストップロスとテイクプロフィットの保護は `StartProtection` を通じて付加されます。距離はpipsで表され、1pipは3桁または5桁の小数点を持つ銘柄では `PriceStep` を10倍したものに相当します。

## 入力
| 名前 | 型 | デフォルト | 説明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1分足 | 計算とシグナルに使用するローソク足シリーズ。 |
| `StopLossPips` | `int` | 30 | ストップロスの距離（pips）。無効にするには0に設定。 |
| `TakeProfitPips` | `int` | 50 | テイクプロフィットの距離（pips）。無効にするには0に設定。 |
| `StartHour` | `int` | 8 | 取引セッション開始を示す時間（含む、0-23）。 |
| `EndHour` | `int` | 21 | 取引セッション終了を示す時間（含まない、0-23）。`StartHour` より大きくなければなりません。 |
| `DifferenceThreshold` | `decimal` | 0.0001 | 方向性シグナルをトリガーする比較されたMA値間の最小絶対差。 |
| `BarA` | `int` | 0 | MAの比較に使用する最初のバーのインデックス（0 = 現在のローソク足）。 |
| `BarB` | `int` | 1 | MAの比較に使用する2番目のバーのインデックス。 |
| `ReverseSignals` | `bool` | `false` | 強気と弱気の条件を反転します。 |
| `CloseOppositePositions` | `bool` | `false` | 有効にすると、新しいトレードを開く前に反対方向のポジションを閉じるために注文サイズを増加させます。 |
| `MaShift` | `int` | 0 | 移動平均線に適用する水平シフト。正の値は古いMAポイントにアクセスします。 |
| `MaLength` | `int` | 55 | 移動平均線の期間。 |
| `MaMethod` | `MovingAverageMethods` | `Exponential` | 平滑化手法（`Simple`、`Exponential`、`Smoothed`、`Weighted`）。 |
| `AppliedPrice` | `AppliedPriceTypes` | `Median` | MAの入力として使用する価格（`Close`、`Open`、`High`、`Low`、`Median`、`Typical`、`Weighted`）。 |

## ポジション管理
- 基本取引サイズを制御するために戦略の `Volume` を設定します。`CloseOppositePositions` がアクティブな場合、現在のポジションと組み合わされます。
- ストップロスとテイクプロフィットの保護はオプションです。それぞれのpips距離がゼロより大きい場合にのみ付加されます。

## 注意事項
- 取引ウィンドウは銘柄の時間で動作します。`[StartHour, EndHour)` 外のシグナルはスキップされます。
- `MaShift` が負のインデックスを生成する場合、戦略は十分な履歴が蓄積されるまで待機します。これはシフトされたバッファが `EMPTY_VALUE` を返す可能性がある元のEAの動作を反映しています。
- 元のエキスパートは差の閾値が満たされない場合は常に売り注文をデフォルトとするため、変換された戦略は完全な忠実性のために同じロジックを維持します。この動作が望ましくない場合は `DifferenceThreshold` を調整してください。
