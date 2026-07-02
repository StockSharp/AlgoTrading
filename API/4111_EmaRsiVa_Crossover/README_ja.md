# EMA RSI ボラティリティ適応クロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTrader エキスパート アドバイザー **EA_MARSI_1-02** の直接移植です。の2つのコピー間のクロスオーバーを交換します。
Integer のカスタム *EMA_RSI_VA* インジケーター、相対強度指数 (RSI) によって駆動されるボラティリティ適応型移動平均。
低速ラインが高速ラインを横切るたびに、エンジンはネット位置を逆転させ、元の「フリップオンクロスオーバー」を再現します。
StockSharp の注文処理のベスト プラクティスを尊重しながらの行動。

## インジケーターの仕組み

オリジナルの MQL パッケージには、`EMA_RSI_VA` というカスタム インジケーターが同梱されています。価格平滑化された EMA を計算します。
長さは、中立値からの RSI の距離によって調整されます。 StockSharp ポートは、
数式を正確に複製する `EmaRsiVolatilityAdaptiveIndicator` クラス:

1. 選択した `AppliedPrice` ソースの RSI を期間 `RSIPeriod` で計算します。
2. 50 (`|RSI - 50| + 1`) からの RSI の距離を測定します。これはボラティリティのプロキシとして機能します。
3. 適応乗算器を導出する
`multi = (5 + 100 / RSIPeriod) / (0.06 + 0.92 * dist + 0.02 * dist^2)`。
4. 構成された EMA 期間にこの乗数を乗じて、動的な長さ `pdsx` を取得します。
5. ローソク足の適用価格を入力として使用し、平滑化係数 `2 / (pdsx + 1)` を使用して標準の EMA 再帰を適用します。

RSI の偏位が大きいと、スムージング ウィンドウが短くなり、ラインの反応が速くなります。フラットな RSI は窓を長くし、湿気を軽減します
騒音。低速回線と高速回線の両方で、`StockSharp.Messages.AppliedPrice` でサポートされる価格モードの完全なセットが公開されます。

## 取引ルール

- **信号検出**
  - *売り/空売り*: 以前のスロー < 以前の速い ** および ** 現在のスロー ≧ 現在の速い。
  - *買い/ロング*: 以前のスロー > 以前のファースト ** および ** 現在のスロー ≤ 現在のファースト。
- **実行**
  - この戦略は、構成されたキャンドル シリーズから完成したキャンドルのみを分析します。
  - シグナルが発生すると、既存のエクスポージャーをクローズし、新しい方向を開くためのサイズの成行注文が送信されます。
  - 交換制限は、`Security.MinVolume`、`Security.VolumeStep`、および `Security.MaxVolume` を通じて尊重されます。
- **逆転**
  - 注文はネッティングされるため、単一の `SellMarket` または `BuyMarket` 呼び出しがゼロラインを越えてポジションを取得し、
MQL 反対のシグナルが即座に取引を反転させる動作。

## リスク管理

- `TakeProfitPoints` と `StopLossPoints` は、エキスパート アドバイザーの TP/SL フィールド (価格ポイントで表現) を複製します。どちらかのとき
値がゼロ以外の場合、戦略は絶対価格オフセットと `useMarketOrders = true` を使用して StockSharp の保護マネージャーを開始します。
元の `OrderSend` ストップ/リミット変更ループをミラーリングします。
- `UseBalanceMultiplier` は `use_Multpl` トグルを実装します。有効な場合、有効注文量は次のようになります。
`Volume * PortfolioEquity / MaxDrawdown` には、制約を交換するための防御クランプが付いています。
- 基本クラスの `StartProtection()` 呼び出しは引き続き実行されるため、外部リスク モジュールがトレーリングまたは損益分岐点を付加できるようになります。
必要に応じてロジックを変更します。

## パラメーター

| パラメータ | デフォルト | 説明 |
|-----------|---------|-------------|
| `Volume` | `0.1` | 残高乗数が適用される前のベース成行注文サイズ。 |
| `TakeProfitPoints` | `0` | 商品ポイントでの利食い距離。 `0` はテイクプロフィットレッグを無効にします。 |
| `StopLossPoints` | `0` | 計器ポイントでのストップロス距離。 `0` は保護停止を無効にします。 |
| `UseBalanceMultiplier` | `false` | EA の `use_Multpl` と同じ残高比例ポジションサイジングを有効にします。 |
| `MaxDrawdown` | `10000` | バランス乗数の分母。 EA の `Max_drawdown` に対応します。 |
| `SlowRsiPeriod` | `310` | RSI は遅い EMA_RSI_VA 回線をルックバックします。 |
| `SlowEmaPeriod` | `40` | RSI を適応させる前の低速回線の基本 EMA の長さ。 |
| `SlowAppliedPrice` | `Close` | 価格モードは低速インジケーターに転送されます。 |
| `FastRsiPeriod` | `200` | RSI は高速な EMA_RSI_VA 行をルックバックします。 |
| `FastEmaPeriod` | `50` | RSI を適応させる前の高速回線の基本 EMA の長さ。 |
| `FastAppliedPrice` | `Close` | 価格モードは高速インジケーターに転送されます。 |
| `CandleType` | `TimeFrame(1m)` | 計算に使用されるローソク足シリーズ。 |

## 実装メモ

- ポートは、手動インジケーターのループを避けるために、StockSharp の高レベルの API (`SubscribeCandles().Bind(...)`) で書き込まれます。
- MQL ソース内の `CopyBuffer(..., 1, 2, ...)` 呼び出しと一致する、完了したキャンドルのみが処理されます。
- ボリュームの正規化では、`Security.MinVolume`、`Security.VolumeStep`、および `Security.MaxVolume` を使用し、無効な注文を防ぎます。
本当のやりとり。
- Python バージョンは、要求に応じて意図的に省略されています。このディレクトリには C# 実装とドキュメントのみが含まれます。

結果として得られる動作は、ソース EA を反映しながら、StockSharp に適したパラメータとリスク制御を公開します。
デザイナー、ランナー、または StockSharp API 上に構築されたカスタム ホスト。
