# FT Bill Williams トレーダー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

**FT Bill Williams トレーダー戦略** は、MetaTrader エキスパート アドバイザー「FT_BillWillams_Trader」の高レベルの StockSharp 翻訳です。 Bill Williams フラクタルと Alligator インジケーターを組み合わせて、トレンドのブレイクアウトをトレードします。この戦略は、新しいフラクタルを監視し、Alligator 構造がブレイクアウト方向を確認していることを検証し、オプションでポジションを開く前に距離、アライメント、およびリバースシグナルフィルターを適用します。

## 取引ロジック

1. **フラクタル検出** – この戦略は、最新の `FractalPeriod` の高値と安値をバッファーします。中央のバーがウィンドウ内の最高 (または最低) ポイントになると、新しいブレイクアウト レベルが記録されます。時期尚早のエントリを避けるために、フラクタルの上下に `IndentPoints` オフセットが追加されます。
2. **ブレイクアウトの確認** – `EntryConfirmation` に応じて:
   - `PriceBreakout` は、ローソク足の範囲がブレイクアウトレベルを横切るときを確認します。
   - `CloseBreakout` は、前のローソク足の終値がそのレベルを超えるまで待ちます。
3. **距離チェック** – ブレイクアウト レベルが Alligator リップ (以前のバー値) から `MaxDistancePoints` よりも遠い場合、エントリーは拒否されます。フィルターを無効にするには、距離をゼロに設定します。
4. **歯フィルタ** – `UseTeethFilter` が有効な場合、前のクローズは Alligator の歯の上（ロングの場合）または下（ショートの場合）にある必要があります。
5. **トレンドの調整** – `UseTrendAlignment = true` では、唇、歯、顎はそれぞれ少なくとも `TeethLipsDistancePoints` と `JawTeethDistancePoints` ポイント離れている必要があり、Alligator がトレンドであることを確認します。
6. **リバース イグジット** – `ReverseExit = OppositeFractal` の場合、新しい反対のフラクタルはオープン ポジションを即座にクローズします。 `OppositePosition` の場合、この戦略はまず現在の取引を終了してから、反対方向の取引を開始します。
7. **ジョー出口** – `JawExit` は、価格が Alligator ジョー（バー内またはローソク足終値）を横切ったときにポジションをクローズするかどうかを定義します。
8. **トレーリング ストップ** – `EnableTrailing` が true で、取引に利益がある場合、唇と `SlopeSmaPeriod` SMA の相対的な傾きに応じて、ストップは唇または歯に移動します。最初の保護ストップと利益目標は、`StopLossPoints` と `TakeProfitPoints` によって制御されます。

## パラメーター

| プロパティ | 説明 | デフォルト |
|----------|-------------|---------|
| `OrderVolume` | 成行注文を送る際に使用される取引量。 | `0.1` |
| `FractalPeriod` | フラクタル パターンのバーの数 (奇数の値を推奨)。 | `5` |
| `IndentPoints` | ブレークアウト レベルに追加されるオフセット (ポイント単位)。 | `1` |
| `EntryConfirmation` | ブレークアウト確認モード (`PriceBreakout`、`CloseBreakout`)。 | `CloseBreakout` |
| `UseTeethFilter` | 以前の歯が Alligator の歯の正しい側にある必要があります。 | `true` |
| `MaxDistancePoints` | ブレイクアウト レベルと Alligator 個の唇 (ポイント) の間の最大距離。 | `1000` |
| `UseTrendAlignment` | Alligator 行間の最小間隔を強制します。 | `false` |
| `JawTeethDistancePoints` | アライメントフィルターで使用される顎と歯の最小距離。 | `10` |
| `TeethLipsDistancePoints` | アライメントフィルターで使用される歯と唇の最小距離。 | `10` |
| `JawExit` | ジョークロスオーバーでポジションをクローズするモード (`Disabled`、`PriceCross`、`CloseCross`)。 | `CloseCross` |
| `ReverseExit` | 逆信号処理 (`Disabled`、`OppositeFractal`、`OppositePosition`)。 | `OppositePosition` |
| `EnableTrailing` | Alligator ベースのトレーリング ストップ管理を有効にします。 | `true` |
| `SlopeSmaPeriod` | 唇の傾きと比較されるSMAの周期。 | `5` |
| `StopLossPoints` | ポイント単位のストップロス距離 (0 無効)。 | `50` |
| `TakeProfitPoints` | テイクプロフィット距離 (ポイント単位) (0 無効)。 | `50` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Alligator 行のピリオド。 | `13`, `8`, `5` |
| `JawShift`, `TeethShift`, `LipsShift` | 各 Alligator 行の前方シフト。 | `8`, `5`, `3` |
| `MaMethod` | Alligator (`Simple`、`Exponential`、`Smoothed`、`Weighted`) の移動平均タイプ。 | `Simple` |
| `AppliedPrice` | Alligator に提供されたローソク足の価格。 | `CandlePrice.Median` |
| `CandleType` | マーケットデータからサブスクライブされたローソク足タイプ。 | `15-minute timeframe` |

## 追加メモ

- この戦略は、デフォルトのチャート領域で Alligator ラインを描画し、取引を実行します。
- `FractalPeriod` は、中央のバーがフラクタルの頂点を表すように奇数のままにする必要があります。デフォルト値は元の Expert Advisor と一致します。
- 距離ベースのパラメータ (`IndentPoints`、`MaxDistancePoints`、`JawTeethDistancePoints`、`TeethLipsDistancePoints`、`StopLossPoints`、`TakeProfitPoints`) はブローカー ポイント (`Security.PriceStep`) で表されます。
- トレーリングストップとジョーイグジットは完了したローソク足に依存し、Alligator の前のバー値で動作する元の MQL ロジックを反映しています。
