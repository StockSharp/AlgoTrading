# 手作業で構築する保護注文ストラテジーダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、確定した EMA(14)/EMA(50) のクロスでポジションを開始または完全反転し、注文ライフサイクルの明示的なブロックで保護処理を構築します。受理されたエントリー約定が両方の保護水準を固定します。100本のローソク足のクールダウン後、エントリー価格基準の利確指値を一度だけ登録します。その後の差し替えでも同じ目標価格を維持し、ストップまたは条件を満たす反転では、まず稼働中の利確注文の取消要求を送り、取消確認を待たずに成行でのポジション変更を起動します。

![schema](schema.svg)

## 戦略の概要

- 確定済み5分足が、形成済み値だけを出力する高速 EMA 14 と低速 EMA 50 に入ります。高速 EMA が低速 EMA を上抜けると Crossing は上方向イベントを、下抜けると下方向イベントを出力します。
- 上方向クロスは Position <= 0 の場合だけ、下方向クロスは Position >= 0 の場合だけ動作できます。ポジション比較はフラットのシグナルを固定 Volume の成行注文1件へ送ります。反対ポジションでは、反転経路が同じ Volume の成行注文2件を順に起動します。1件目が既存側を閉じ、2件目は1件目の約定を待たず直ちに新しい側を開きます。
- Strategy trades の新しい約定ごとにカウンターを 0 にリセットし、クールダウンを再開始します。続く確定済み100本では、新規エントリー、利確の登録と差し替え、ストップ判定を停止し、その後に別の約定でウィンドウが再開始されなければ101本目に処理を再開します。
- クールダウンが完了すると、一回限りの Flag が固定 Volume の利確を登録します。この初回登録に注文板の利用可能状態は不要で、必要な側の状態は確定済み足で時刻付けされる後続の差し替えだけを許可します。ロングは Entry Price * (1 + Take Profit fraction) の売り指値、ショートは Entry Price * (1 - Take Profit fraction) の買い指値です。
- Combination は最初の登録 Order と差し替えクローンを1つの注文ストリームへ統合するだけです。接続された replaceOrder.order と cancelOrder.order の入力が、最後に転送された注文を記憶します。注文板分岐は意図的に簡略化されています。BestBid と BestAsk は、確定済み足で時刻付けされる同じ固定目標での後続差し替えだけを許可し、目標を決して移動しません。ストップまたは条件を満たす反転は、まず取消を要求し、確認を待たずに成行処理を起動します。

## エントリーとエグジットの条件

- **ロングエントリー**: 確定した EMA 上方向クロスで Position <= 0 かつクールダウン完了なら、フラットからは Volume の成行買いを1件送ります。ショートからはまずショート側利確の取消を要求し、同じ Volume の成行買いを2件起動します。1件目がショートを閉じ、2件目は閉じる約定を待たず直ちにロングを開きます。Entry Price はフラットからの開始約定、または反転の2件目である開始約定だけから取得します。
- **ショートエントリー**: 確定した EMA 下方向クロスで Position >= 0 かつクールダウン完了なら、フラットからは Volume の成行売りを1件送ります。ロングからはまずロング側利確の取消を要求し、同じ Volume の成行売りを2件起動します。1件目がロングを閉じ、2件目は閉じる約定を待たず直ちにショートを開きます。Entry Price はフラットからの開始約定、または反転の2件目である開始約定だけから取得します。
- **エグジット**: クールダウン後、ロング側は Entry Price * (1 + 0.006) に固定 Volume の売り指値を1件、ショート側は Entry Price * (1 - 0.006) に固定 Volume の買い指値を1件保持します。確定終値が Entry Price * (1 - 0.003) 以下ならロングを停止し、Entry Price * (1 + 0.003) 以上ならショートを停止します。ストップはまず稼働中の利確注文の取消を要求し、確認を待たず同じ固定 Volume の反対向き成行注文を1件送ります。条件を満たす反対クロスも、2注文の反転前に同じ要求後起動ルールを使います。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 00:05:00 | 5分時間枠。EMAシグナル、クールダウン計数、ストップ判定、注文ライフサイクルのタイミングは確定済み足だけで進みます。 |
| Fast EMA Length | 14 | 高速 ExponentialMovingAverage の長さ。形成済み値だけを出力します。 |
| Slow EMA Length | 50 | 低速 ExponentialMovingAverage の長さ。形成済み値だけを出力します。 |
| Take Profit fraction | 0.006 | Entry Price から有利方向への比率です。0.006 は 0.6% で、ストップ対利確を 1:2 にします。 |
| Stop fraction | 0.003 | Entry Price から不利方向への比率です。0.003 は 0.3% です。 |
| Volume | 1 | 各エントリー部分、利確の登録と差し替え、成行ストップで使う固定数量です。反転では同じ Volume の成行注文を2件に分け、先に閉じ、次に開きます。 |
| Cooldown | 100 | Strategy trades の各約定後に停止する確定済み足の本数です。新しい約定ごとにカウンターを 0 にリセットし、その後に別の約定でウィンドウが再開始されなければ101本目に処理を再開します。 |

## ダイアグラムの詳細

- [ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは確定済み5分足だけを出力します。終値コンバーターと、形成済み値に限定した2つの[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックが、終値、ExponentialMovingAverage 14、ExponentialMovingAverage 50 を供給します。
- [クロス](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)、[ポジション](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/current.html)、[比較](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)が、対称な Position <= 0 と Position >= 0 のエントリーゲートを構成します。
- 個別の[ポジション変更](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ブロックが、フラットからの OpenPosition と、反転の NoCondition による終了部分・開始部分を実装します。全ブロックが同じ固定 Volume を受け取ります。終了部分を先に起動しますが、その約定を待たず開始部分を直ちに起動するため、非同期実行では競合が起こり得ます。Formula ブロックはクールダウン状態と利確・損切り値を計算し、注文数量は決して計算しません。
- [ストラテジー取引](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html)は、自身の各約定後のクールダウン再開始とチャートへの取引出力だけに使われます。フラット開始ブロックの trade 出力と、反転の2番目である開始部分の trade 出力だけが、受理された Trade.Price を対応するロングまたはショート保護状態へ渡します。反転の1番目である終了約定は除外されます。
- [板情報](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/market_depth.html)は BestBid と BestAsk の利用可能状態を供給します。どちらの気配値も利確価格式には入らず、ロングとショートの目標は Entry Price に固定されます。
- ロングとショートの価格式は、それぞれ指値の[注文登録](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/orders/register.html)ブロックに入ります。Flag はクールダウン後に1回の登録だけを許可し、その後の各差し替えは同じ計算価格と同じ固定 Volume を再利用します。
- 最初の登録 Order と各差し替えクローンは Combination<Order> バスへ入り、バスはそれらを統合して転送するだけです。接続された replaceOrder.order 入力と[注文取消](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html)ブロックの cancelOrder.order 入力が、最後に転送された注文を記憶します。再登録の処理中は、取消が以前に転送された注文参照をまだ対象にする場合があります。ストップまたは反転では、まず取消要求を送り、その直後に取消確認を待たず固定 Volume の成行ストップまたは固定 Volume の反転2部分を起動します。この順序の原子性は保証されません。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
