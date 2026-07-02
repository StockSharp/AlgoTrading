# ナツセコプロトレーダー4H戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Natuseko Protrader 4H 戦略は、MetaTrader 4 エキスパート アドバイザー *NatusekoProtrader4HStrategy* の StockSharp 移植です。オリジナル
ロボットは、指数移動平均、Bollinger バンドでフィルタリングされた MACD オシレーター、RSI しきい値、および Parabolic SAR を組み合わせて、
4時間足の強いブレイクアウトローソク足を特定します。適格なローソクが現れると、システムはすぐに開くか、
高速の EMA への引き戻しを待ってから入ります。一度配置されると、戦略は部分的な利益確定と完全な撤退を実行します。
RSI および Parabolic SAR 信号に基づいて、MQL コードに存在する資金管理ブロックを複製します。

## 取引ロジック
1. `CandleType` で定義されたプライマリ ローソク足ストリーム (デフォルトでは 4 時間足のローソク足) をサブスクライブし、完了したローソク足のみを処理します。
2. 終値の 3 つの指数移動平均 (高速、低速、トレンド) を計算します。 3 つすべての長さを設定できます。
3. MACD インジケーター (EA から取得した高速、低速、シグナル期間) をフィードし、単純移動平均と Bollinger バンドを適用します。
MACD 本線。 Bollinger の正中線は、MQL バージョンで使用される基準レベルとして機能します。
4. 完全なローソク足データを使用して、終値の RSI と Parabolic SAR を計算します。これらのインジケーターはエントリーとエグジットの両方を推進します。
5. 次の条件がすべて当てはまる場合に、強気のセットアップローソク足を検出します。
   - 高速 EMA は低速とトレンドの EMA を上回っています。
   - RSI は `RsiEntryLevel` より上ですが、`RsiTakeProfitLong` より下です。
   - MACD の主線は、短い SMA と Bollinger の正中線の両方の上にあります。 SMA も正中線より上にあります。
   - ローソク本体は両方の影よりも大きいため、ローソクは動きの方向に強く閉じることになります。
   - Parabolic SAR はローソクの終値の下に座っています。
6. 対称チェックを使用して弱気セットアップを検出します (以下の高速 EMA、`RsiTakeProfitShort` と `RsiEntryLevel` の間の RSI、MACD の値)
Bollinger 正中線の下、弱気のローソク体、終値の上の SAR）。
7. 適格なローソク足がトレンド EMA から遠すぎる場合 (`DistanceThresholdPoints` を超える距離)、保留フラグを設定し、
引き戻し。 RSIとSARが強気シナリオに沿ったままである間に、価格が速いEMAに達するとロングエントリーがトリガーされます。の
ショートエントリーは、下からの高速な EMA への引き戻しと同様に機能します。
8. プルバックが必要ない場合、この戦略は反対のエクスポージャーをクローズし、`TradeVolume` ロットで新しいポジションをオープンします。ストップロス
配置は EA ルールに従います。`UseSarStopLoss` が有効な場合は Parabolic SAR が最優先され、それ以外の場合はトレンドが優先されます。
EMA が使用されます。 `StopOffsetPoints` は商品価格ステップで価格距離に変換され、ストップレベルに適用されます。
9. ロングポジションがオープンしている間、ストラテジーは継続的にストップ価格を再計算し、エグジットを管理します。
   - 価格がストップを下回った場合、ポジション全体がクローズされます。
   - 少なくとも `MinimumProfitPoints` の利益 (商品ポイント単位) に達した後、戦略はポジションの半分を閉じることができます。
RSI が `RsiTakeProfitLong` を超えるか、Parabolic SAR が価格を上回ったとき (`UseRsiTakeProfit` によって制御され、
`UseSarTakeProfit`)。
   - 十分な利益が得られ、RSI が `RsiEntryLevel` を下回ると、残りのロングエクスポージャーは終了します。
10. ショート ポジションは同じルールを反映しており、RSI のしきい値が反転され、SAR チェックが価格に対して反転されます。

## ポジション管理
- 部分的なエグジットは取引サイドごとに最大 1 回発生します。ポジションの半分を閉じた後、ストラテジーは完全終了条件を待ちます。
(RSI がニュートラルレベルを通過するかストップロスヒット)。
- ストップロス価格は、MQL ロジックとの整合性を保つために、最新の Parabolic SAR またはトレンド EMA の値を使用してローソク足ごとに再計算されます。
- ポジションサイズがゼロに戻ると、内部状態 (保留エントリーフラグ、ストップ参照、部分出口マーカー) がリセットされるため、
次の取引はきれいに始まります。

## パラメーター
| 名前 | 種類 | デフォルト | 説明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 4時間枠 | 戦略によって処理される主な時間枠。 |
| `TradeVolume` | `decimal` | `0.1` | エントリーに使用される注文量。 |
| `FastEmaPeriod` | `int` | `13` | 高速 EMA フィルターの長さ。 |
| `SlowEmaPeriod` | `int` | `21` | 低速な EMA フィルターの長さ。 |
| `TrendEmaPeriod` | `int` | `55` | EMA は距離チェックとストップロスの配置に使用されます。 |
| `MacdFastPeriod` | `int` | `5` | MACD インジケーター内の高速な EMA の長さ。 |
| `MacdSlowPeriod` | `int` | `200` | MACD インジケーター内の EMA の長さが遅いです。 |
| `MacdSignalPeriod` | `int` | `1` | MACD インジケーター内のシグナル移動平均の長さ。 |
| `BollingerPeriod` | `int` | `20` | Bollinger バンドの計算に使用される MACD サンプルの数。 |
| `BollingerWidth` | `decimal` | `1` | MACD Bollinger バンドの標準偏差乗数。 |
| `MacdSmaPeriod` | `int` | `3` | MACD スムージング SMA の長さ。 |
| `RsiPeriod` | `int` | `21` | RSI インジケーターの長さ。 |
| `RsiEntryLevel` | `decimal` | `50` | 開始ルールと終了ルールによって共有される中立の RSI しきい値。 |
| `RsiTakeProfitLong` | `decimal` | `65` | ロングポジションの部分利益確定を可能にするRSIレベル。 |
| `RsiTakeProfitShort` | `decimal` | `35` | RSI レベルでショートポジションの部分利益確定が可能になります。 |
| `DistanceThresholdPoints` | `decimal` | `100` | エントリーが遅れるまでの、価格とトレンド EMA の間の商品ポイント単位の最大距離。 |
| `SarStep` | `decimal` | `0.02` | Parabolic SAR の加速ステップ。 |
| `SarMaximum` | `decimal` | `0.2` | Parabolic SAR の最大加速度。 |
| `UseSarStopLoss` | `bool` | `false` | Parabolic SAR を使用して保護停止を取得します。 |
| `UseTrendStopLoss` | `bool` | `true` | トレンド EMA を使用して保護ストップを導き出します。 |
| `StopOffsetPoints` | `int` | `0` | 追加のオフセット (ポイント単位) が保護ストップ価格に追加されます。 |
| `UseSarTakeProfit` | `bool` | `true` | 価格が Parabolic SAR を超えたときに部分的な決済を有効にします。 |
| `UseRsiTakeProfit` | `bool` | `true` | RSI がテイクプロフィットしきい値に達したときに、部分的なエグジットを有効にします。 |
| `MinimumProfitPoints` | `decimal` | `5` | 部分的または完全な利益確定ルールが有効になる前の最小利益 (ポイント単位)。 |

## オリジナルの EA との違い
- StockSharp はネットポジションを取引します。 MetaTrader のシングルチケットの動作をエミュレートするために、戦略は反対のチケットを自動的にクローズします
反対方向に新しい取引を開始する前にエクスポージャーを確認してください。
- StockSharp が管理しないため、資金管理ヘルパーは個々の注文を変更するのではなく成行注文で実装されます。
チケットごとに停車します。この効果は EA と一致します。1 回の部分的な終了に続いて、RSI の勢いが弱まったときに最終的な終了が続きます。
- 価格距離の計算は商品 `PriceStep` に依存します。証券が価格ステップを定義していない場合、戦略は次のように仮定します。
ステップ 1. 異なるポイント サイズを使用する楽器に応じて、`DistanceThresholdPoints` と `MinimumProfitPoints` を調整します。

## 使い方のヒント
- 機器のロットステップに従って `TradeVolume` を設定します。コンストラクターは同じ値を `Strategy.Volume` にも割り当てます。
ヘルパー メソッドは予想されるサイズを使用します。
- ローソク足がトレンド EMA から遠く離れて終了するために取引が頻繁に遅れる場合は、`DistanceThresholdPoints` を下げるか、フィルターを無効にしてください
それをゼロに設定します。
- 戦略をチャート化することをお勧めします。コードはローソク足、3 つの EMA、RSI、Parabolic SAR、および MACD Bollinger バンドを描画します。
変換されたロジックを視覚的に確認します。
- MACD パラメータは、EA の珍しい組み合わせ (fast=5、slow=200、signal=1) を反映しています。本番稼働前に最適化を検討してください
これは、このような広い低速期間により、非常に滑らかではあるものの遅れのある値が生成されるためです。
