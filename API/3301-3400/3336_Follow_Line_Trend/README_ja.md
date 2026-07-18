# ライントレンド戦略に従う
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Follow Line 戦略は、MetaTrader エキスパート アドバイザー `FollowLineEA_v1.0` を直接移植したものです。 Bollinger バンド ブレイクアウト検出器と価格変動に追従する適応トレンド ラインを組み合わせることで、元のロジックを再現します。この戦略は、完成したキャンドルをリッスンし、ユーザーが指定した任意の時間枠で機能します。

Bollinger バンドの上部を上抜けると価格の下のサポートラインが引き上げられ、下側バンドを下回る終値は価格の上のレジスタンスラインを引き下げます。ラインはブレイクアウト方向にのみスライドし、持続的なトレンドを強調する階段パターンを作成します。オプションの ATR パディングを使用すると、ポジションが早期にトリガーされないようにするためにラインを広げることができます。移動平均に基づくモメンタムフィルターは、選択した矢印モードに応じてエントリーを確認します。

## 取引ロジック
1. **インジケーターチェーン**
   - Bollinger バンド（長さ = `BollingerPeriod`、幅 = `BollingerDeviations`）。
   - `UseAtrFilter` が有効な場合に傾向線をオフセットするためのオプションの ATR (長さ = `AtrPeriod`)。
   - 高値、安値、始値、終値、中央値に適用される単純移動平均のファミリー (長さ = `MovingAveragePeriod`)。これらの平均は、`TypeOfArrows` が `OpenCloseMedian` または `HighLowOpenClose` に設定されている場合に確認フラグを生成します。
2. **トレンドラインの更新**
   - 上部バンドの上で閉じるローソク足は、トレンド ラインをローソク足の安値 (使用する場合はマイナス ATR オフセット) まで押し上げますが、それを下げることはありません。
   - ローソク足が下側のバンドの下で閉じると、ラインがローソク足の高さ (使用されている場合はさらに ATR オフセット) まで引っ張られますが、ラインが持ち上げられることはありません。
   - 傾向線の方向は、市場が強気 (>0) と見なされるか弱気 (<0) と見なされるかを定義します。
3. **エントリーシグナル**
   - 方向が弱気から強気に切り替わり、矢印フィルターが一致すると、買いの矢印がキューに追加されます。
   - 方向が強気から弱気に反転すると、売り矢印がキューに追加されます。
   - `IndicatorsShift` パラメータは実行を遅らせ、矢印が形成された後に `IndicatorsShift` バー処理できるようにし、MT4 バッファ シフトを模倣します。
4. **実行フィルター**
   - 時間フィルター: `UseTimeFilter` が有効な場合、取引は `TimeStartTrade` と `TimeEndTrade` の間でのみ許可されます (ウィンドウは午前 0 時をラップすることができます)。
   - スプレッド フィルター: 現在のスプレッドが `MaxSpread` (価格ステップで測定) を超える場合、注文はスキップされます。
   - 注文上限: `MaxOrders` は、元の「最大注文」チェックを再現するために絶対ポジション サイズを制限します。

## リスク管理
- **反対信号で終了**: `CloseInSignal` を `true` に設定すると、反対の矢印が発射されたときに既存の露出が直ちに平坦になります。
- **バスケット ロック**: `CloseInProfit` および `CloseInLoss` は、指定されたピップ ターゲットに到達すると、現在の位置を閉じます。 `UseBasketClose` は、長いロジックと短いロジックを分離するのではなく、バスケット全体にしきい値を適用します (MQL の実装を反映しています)。
- **ストップとターゲット**: ストラテジーは、対応するトグルが有効になっているとき (`UseStopLoss`、`UseTakeProfit`、`UseTrailingStop`、`UseBreakEven`) に対応するバーごとに、`SetStopLoss`、`SetTakeProfit` を呼び出し、トレーリングおよび損益分岐点をガードします。すべての距離は価格ステップで表されます。
- **ロットサイジング**: `AutoLotSize` が有効な場合、ポジション サイズは現在のポートフォリオ値の選択されたシェア (`RiskFactor` パーセント) と等しくなります。それ以外の場合は、固定の `ManualLotSize` が使用されます。金額は機器のボリュームステップに正規化され、交換制限によって制限されます。

## パラメーター
| グループ | 名前 | 説明 |
| --- | --- | --- |
| 一般 | `CandleType` | サブスクリプションに使用されるタイムフレームまたはカスタム キャンドル タイプ。 |
| インジケーター | `BarsCount` | インジケーターによって使用される履歴の深さ。 |
| インジケーター | `BollingerPeriod` / `BollingerDeviations` | ブレークアウト検出のための Bollinger 構成。 |
| インジケーター | `MovingAveragePeriod` | 矢印フィルタに影響を与える移動平均の長さ。 |
| インジケーター | `AtrPeriod` / `UseAtrFilter` | ATR の長さとアクティブ化フラグ。 |
| インジケーター | `TypeOfArrows` | アローモード (`HideArrows`、`SimpleArrows`、`OpenCloseMedian`、`HighLowOpenClose`)。 |
| インジケーター | `IndicatorsShift` | 矢印の形成と実行の間の遅延 (バー単位)。 |
| 時間 | `UseTimeFilter`, `TimeStartTrade`, `TimeEndTrade` | セッション制限。 |
| フィルター | `MaxSpread`, `MaxOrders` | スプレッド天井とポジション制限。 |
| リスク | `CloseInSignal`, `UseBasketClose`, `CloseInProfit`, `PipsCloseProfit`, `CloseInLoss`, `PipsCloseLoss` | バスケット管理ルール。 |
| リスク | `UseTakeProfit`, `TakeProfit`, `UseStopLoss`, `StopLoss`, `UseTrailingStop`, `TrailingStop`, `TrailingStep`, `UseBreakEven`, `BreakEven`, `BreakEvenAfter` | 保護注文スイート (価格ステップの値)。 |
| お金の管理 | `AutoLotSize`, `RiskFactor`, `ManualLotSize` | ポジションサイジング。 |

## 使用上の注意
- この戦略は完成したキャンドルに対してのみ機能します。したがって、ライブ取引と同じ足圧縮を使用してバックテストを安全に行うことができます。
- `IndicatorsShift` の背後にあるカスタム キューは、MT4 インジケーター バッファ アクセス (`iCustom(..., shift)`) と同じ高レベルの API 動作を維持します。
- `TypeOfArrows = HideArrows` は、ソース EA とまったく同様に、インジケーター描画ロジックを保持しながら取引を無効にします。
- 取引を視覚化するには、`CreateChartArea()` を呼び出した後、戦略をチャート領域に添付します (`OnStarted` ですでに処理されています)。

## 変換の詳細
- このロジックは、組み込みの StockSharp インジケーターと高レベルのローソク足サブスクリプション API のみに依存します (手動のバッファリングや `GetValue` の呼び出しはありません)。
- 注文管理は、`BuyMarket`/`SellMarket` に加えてヘルパー メソッド `SetStopLoss` および `SetTakeProfit` を使用して行われ、元のコードの MT4 の動作を反映しています。
- ポートフォリオベースのロットサイジングでは、注文を送信する前に、`VolumeStep`、`VolumeMin`、および `VolumeMax` チェックを通じて交換制限が適用されます。
- この戦略では、リポジトリ ガイドラインに合わせて英語のコード コメントとパラメーターの説明が保持されます。
