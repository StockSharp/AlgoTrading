# MA MACD ポジションアベレージング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTraderエキスパートアドバイザー**「MA MACD Position averaging」**の忠実な変換です。加重移動平均フィルターと
MACD比率チェックを組み合わせ、設定可能な数のpips分だけ価格が不利な方向に動くたびにポジションサイズを増やすマーチンゲール式
アベレージングモジュールを追加します。すべてのリスクパラメーターはpips単位で設定され、StockSharpが提供する銘柄メタデータを
使用して内部的に価格オフセットに変換されます。

## 取引ロジック

1. **インジケーター準備**
   - 設定可能な移動平均（`MaPeriod`、`MaMethod`、`MaAppliedPrice`）が完成した足でサンプリングされます。`SignalBar`と`MaShift`
     パラメーターは、MetaTraderが指定した数の足だけ遡り、水平オフセットで移動平均をプロットする機能をエミュレートします。
   - MACDインジケーター（`MacdFastPeriod`、`MacdSlowPeriod`、`MacdSignalPeriod`、`MacdAppliedPrice`）が同じ足で処理されます。
     戦略は、インジケーターAPIを直接呼び出すことなく履歴値にアクセスできるよう、MACDのメインラインとシグナルラインを小さな
     ローリングバッファに保存します。
2. **エントリー条件**
   - **ロング**：両MACD線がゼロ以下で、比率`MACDmain / MACDsignal`が`MacdRatio`以上で、足のクローズがサンプリングされた
     移動平均より上にあり、価格と平均の距離が少なくとも`IndentPips`pipsある。
   - **ショート**：両MACD線がゼロ以上で、比率が`MacdRatio`より上で、足のクローズが移動平均より下にあり、それらの距離が
     少なくとも`IndentPips`pipsある。
   - 新規エントリーは戦略にポジションがないときのみ許可されます。アベレージングサイクルがすでに進行中の場合、シグナルロジックは
     スキップされ、アベレージングルールのみが適用されます。
3. **アベレージングモジュール**
   - ロングポジションが存在し、価格が最良（最低）のロングエントリーから少なくとも`StepLossingPips`下落した場合、戦略は
     追加のロング取引を開き、そのボリュームは最後のレッグボリュームに`LotCoefficient`を掛けた値（銘柄ボリュームステップで
     丸め）に等しい。
   - ショートポジションが存在し、価格が最良（最高）のショートエントリーから少なくとも`StepLossingPips`上昇した場合、同じ
     `LotCoefficient`乗数を使用して新しいショートレッグが追加されます。
   - 両方向でエクスポージャーが検出された場合（通常の条件下では起こらないはず）、戦略は一貫性を回復するためにすべてのレッグを
     即座に閉じます。
4. **保護決済**
   - 各レッグは価格単位（`StopLossPips`、`TakeProfitPips`）で表現された個別のストップロスとテイクプロフィットレベルを保存します。
     完成した各足で、戦略は足の値幅が保存されたレベルのいずれかをクロスしたかどうかを確認し、そうであればレッグを成行注文で
     閉じます。
   - トレーリングストップ（`TrailingStopPips`、`TrailingStepPips`）はオプションです。価格がレッグに有利な方向に
     `TrailingStopPips + TrailingStepPips`動いたら、ストップは現在のクローズから`TrailingStopPips`pips後ろに移動します。
     ストップは価格が少なくとも`TrailingStepPips`pipsの追加進歩をした場合にのみ締め付けられます。
5. **保守管理**
   - ボリューム命令は銘柄のボリュームステップに合わせられ、許容最小値/最大値にクリップされます。戦略は二重処理を避けるために
     完全に形成された足（`CandleStates.Finished`）でのみ実行されます。

## パラメーター

| パラメーター | 型 | デフォルト | 説明 |
|------------|-----|----------|------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | インジケーター計算に使用する時間軸。 |
| `OrderVolume` | `decimal` | `0.1` | 最初のエントリーのベースロットサイズ。 |
| `StopLossPips` | `int` | `50` | pips単位のストップロス距離（0でストップ無効）。 |
| `TakeProfitPips` | `int` | `50` | pips単位のテイクプロフィット距離（0でターゲット無効）。 |
| `TrailingStopPips` | `int` | `5` | pips単位のトレーリングストップオフセット。トレーリングを有効にするには正の値が必要。 |
| `TrailingStepPips` | `int` | `5` | トレーリングストップが再び動く前に必要な追加pip距離。 |
| `StepLossingPips` | `int` | `30` | 新しいアベレージングレッグをトリガーするpips単位の価格後退。 |
| `LotCoefficient` | `decimal` | `2.0` | アベレージング時に前回レッグボリュームに適用される乗数。 |
| `SignalBar` | `int` | `0` | インジケーターサンプリング時に遡る完成足数。 |
| `MaPeriod` | `int` | `15` | 足単位の移動平均の長さ。 |
| `MaShift` | `int` | `0` | 移動平均値に適用される水平シフト（足単位）。 |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | 移動平均の平滑化アルゴリズム（単純、指数、平滑、加重）。 |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | 移動平均の入力として使用される足の価格。 |
| `IndentPips` | `int` | `4` | エントリー前に価格と移動平均の間に必要な最小pip差。 |
| `MacdFastPeriod` | `int` | `12` | MACDフィルターの高速EMAの長さ。 |
| `MacdSlowPeriod` | `int` | `26` | MACDフィルターの低速EMAの長さ。 |
| `MacdSignalPeriod` | `int` | `9` | MACDフィルターのシグナルラインの長さ。 |
| `MacdAppliedPrice` | `AppliedPriceType` | `Weighted` | MACD計算に使用される適用価格。 |
| `MacdRatio` | `decimal` | `0.9` | 取引を許可するために必要な最小MACDメイン/シグナル比率。 |

### pip変換

すべてのpipベースの設定（`StopLossPips`、`TakeProfitPips`、`TrailingStopPips`、`TrailingStepPips`、`StepLossingPips`、
`IndentPips`）は銘柄の`PriceStep`で乗算されます。銘柄が小数点以下3または5桁の場合、小数点以下の分数建値のMetaTraderの
「pip」定義を再現するために値がさらに10倍されます。価格ステップが利用できない場合は、フォールバック値`0.0001`が使用されます。

## 実装上の注意

- StockSharpはネッティングモードで動作するため、戦略はポジションレッグの内部リストを維持します。各レッグは独自のエントリー
  価格、ストップ、テイクレベルを追跡し、アベレージングが元のMetaTrader EAのように動作するようにします。
- 保護注文はソフトウェアでシミュレートされます：足がストップロスまたはテイクプロフィットレベルに触れると、そのバーでポジション
  が成行注文で閉じられます。
- `StepLossingPips`がゼロの場合、アベレージングは自動的に無効になります。そうでない場合、各追加レッグは前のレッグボリューム
  に`LotCoefficient`を掛け、最も近いボリュームステップに切り捨てた値を使用します。
- トレーリングストップの更新は現在の価格プロキシとして足のクローズを使用します。ストップは不利な方向には動かず、価格の進歩が
  `TrailingStopPips + TrailingStepPips`を超えるまで非アクティブのままです。
- インジケーターバッファは`SignalBar`と`MaShift`オフセットを遵守するため、決定ロジックはMetaTraderエキスパートがそのインジ
  ケーターバッファから得るのとまったく同じ値を見ます。
