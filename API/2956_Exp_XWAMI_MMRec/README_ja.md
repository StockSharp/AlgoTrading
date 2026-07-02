# Exp XWAMI MMRec (ID 2956) 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この戦略はMetaTraderのエキスパートアドバイザー**Exp_XWAMI_MMRec**を複製し、カスタムXWAMIモメンタムインジケーターとマネー管理「カウンター」を組み合わせます。モメンタムは現在の価格と`Period`バー前の価格との差として測定されます。その差は4つの設定可能な平滑化段階を経由します。第3および第4段階は元のインジケーターの`Up`および`Down`バッファを形成します。2つのバッファ間のクロスがポジション反転を駆動します。

各段階はいくつかの平滑化アルゴリズムをエミュレートできます：シンプル/指数/スムーズド/線形加重移動平均、Jurik JJMA/JurX、Tillson T3、VIDYA（EMAで近似）、KaufmanのAMA。戦略は単一の集計ポジションで動作し、ロングとショートの両方のトレードをサポートします。最近のトレード結果を`BuyTotalTrigger`/`SellTotalTrigger`ウィンドウと比較し、`BuyLossTrigger`/`SellLossTrigger`に対する損失を数えることで、連続した損失の後にリスクが軽減されます。

保護ストップはMetaTrader実装に従います：`StopLossPoints`と`TakeProfitPoints`はシンボルポイント（`Security.PriceStep`）で測定されます。シグナルタイムフレーム内でストップまたはターゲットに触れると、ポジションは即座にクローズされ、トレード結果はマネー管理履歴に入ります。

## パラメーター

| StockSharpプロパティ | デフォルト | 元の入力 | 説明 |
| --- | --- | --- | --- |
| `CandleType` | H1タイムフレーム | `InpInd_Timeframe` | インジケーター用ローソク足の構築に使用するタイムフレーム。 |
| `Period` | 1 | `iPeriod` | モメンタム計算内の現在価格と比較価格の間の距離（バー単位）。 |
| `Method1` / `Length1` / `Phase1` | `T3`, `4`, `15` | `XMethod1`, `XLength1`, `XPhase1` | 段階1の平滑化メソッド、長さ、フェーズ。フェーズはJurik/JurX/T3でのみ使用。 |
| `Method2` / `Length2` / `Phase2` | `Jjma`, `13`, `15` | `XMethod2`, `XLength2`, `XPhase2` | 第2平滑化段階の設定。 |
| `Method3` / `Length3` / `Phase3` | `Jjma`, `13`, `15` | `XMethod3`, `XLength3`, `XPhase3` | 第3平滑化段階の設定（インジケーター`Up`バッファ）。 |
| `Method4` / `Length4` / `Phase4` | `Jjma`, `4`, `15` | `XMethod4`, `XLength4`, `XPhase4` | 第4平滑化段階の設定（インジケーター`Down`バッファ）。 |
| `AppliedPrice` | `Close` | `IPC` | モメンタム計算に転送される価格ソース。両方のTrendFollowフレーバーとDemark価格を含むすべてのMetaTrader価格オプションが再現されます。 |
| `SignalBar` | 1 | `SignalBar` | クロスを評価するために使用する過去ローソク足のインデックス（`0` = 最新の確定バー）。 |
| `AllowBuyOpen` / `AllowSellOpen` | `true` | `BuyPosOpen`, `SellPosOpen` | それぞれロングまたはショートエントリーを有効にする。 |
| `AllowBuyClose` / `AllowSellClose` | `true` | `BuyPosClose`, `SellPosClose` | 反対のシグナルが現れたときの強制エグジットを有効にする。 |
| `NormalVolume` | `0.1` | `MM` | 利益またはニュートラルシリーズ後に使用するデフォルトのロット/ボリュームサイズ。 |
| `ReducedVolume` | `0.01` | `SmallMM_` | 損失が多すぎた後に適用される削減されたロット。 |
| `BuyTotalTrigger` / `BuyLossTrigger` | `5` / `3` | `BuyTotalMMTriger`, `BuyLossMMTriger` | 検査する最近のロングトレード数と、ロングボリューム削減前のウィンドウ内の最大損失。 |
| `SellTotalTrigger` / `SellLossTrigger` | `5` / `3` | `SellTotalMMTriger`, `SellLossMMTriger` | ショートポジションの同じロジック。 |
| `StopLossPoints` | `1000` | `StopLoss_` | ポイントでのストップロス距離。 |
| `TakeProfitPoints` | `2000` | `TakeProfit_` | ポイントでのテイクプロフィット距離。 |

## 動作

1. 要求されたローソク足シリーズを購読し、確定済みローソク足のみを評価します。
2. 価格差（現在の`AppliedPrice`対`Period`バー前）を計算します。十分な履歴が利用可能な場合、差を4つの平滑化段階に通します。
3. 第3（`Up`）および第4（`Down`）段階の出力を保存します。`SignalBar + 1`（前のバー）で`Up`と`Down`がクロスすると、戦略がバイアスを切り替えます。`Up > Down`の場合、ショートポジションがクローズされ、シグナルバーで`Up <= Down`の場合にロングポジションが開かれます。反対のロジックが弱気シグナルを処理します。
4. ポジションサイズはカウンターによって選択されます：最後の`BuyTotalTrigger`（または`SellTotalTrigger`）トレード利益が検査されます。少なくとも`BuyLossTrigger`（または`SellLossTrigger`）が負の場合、次のトレードは`ReducedVolume`を使用します。そうでなければ`NormalVolume`が使用されます。
5. ロングポジションが存在する場合、ストップロスとテイクプロフィット距離は`Security.PriceStep`と掛けてポイントから価格に変換されます。違反時にはポジションがストップ/ターゲット価格でクローズされ、トレードがマネー管理モジュール用に記録されます。ショートトレードは対称的なルールに従います。

## MetaTraderバージョンとの違い

- StockSharpはポジションを集計するため、`BuyMagic`/`SellMagic`、MetaTraderのグローバル変数会計、および`MarginMode`オプションは不要で省略されました。
- Tillson T3は明示的に実装されています；Jurik JJMAとJurXはどちらも提供されたフェーズで`JurikMovingAverage`にマップされます。VIDYAとParMAはStockSharpにネイティブの同等物がないため、指数移動平均で近似されます。
- 注文は`BuyMarket`/`SellMarket`で実行され、ストップ/ターゲットはネイティブのMT5ストップ注文の代わりにローソク足の高値/安値を監視することで適用されます。
- 偏差/スリッページ入力はStockSharpの実行モデルでは不要であり、削除されました。

## 使用上の注意

1. インストルメントを選択し、元のエキスパートが使用するタイムフレームに`CandleType`を設定します。
2. MetaTraderインジケーター設定に合わせて平滑化メソッドと長さを設定します。
3. 希望のリスクポリシーに合わせて`NormalVolume`、`ReducedVolume`、およびトリガー閾値を調整します。
4. ポートフォリオに戦略をアタッチして開始します。取引は完全に自動化されており、すべてのインジケータークロスで反転します。

さらにカスタマイズするには、`ExpXwamiMmRecStrategy.CreateFilter`内の平滑化マッピングを編集して、代替のStockSharpインジケーターを接続できます。
