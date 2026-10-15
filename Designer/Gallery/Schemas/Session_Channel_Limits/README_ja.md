# セッションチャネル指値注文戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、9時間の価格チャネルを毎日1回固定し、その上下境界にクライアント側で管理するOCO指値注文ペアを待機させます。約定後にダイアグラムのロジックが反対注文の取消を要求する方式であり、取引所のアトミックなOCO指図ではありません。確定済み5分足がチャネルを定義し、継続的なBestBid購読が約定用の気配イベントを供給します。次の日次リセットでは残存注文を取り消し、Order Volume数量のReduceOnly成行処理でポジションを調整します。

![schema](schema.svg)

## 戦略の概要

- 確定済み5分足が、形成済み値だけを出力するHighest 108とLowest 108に入ります。日次スナップショット時の移動窓には、OpenTimeがUTC 01:00から09:55までの足が入り、01:00–10:00の完全なセッションを表します。
- 注文用の稼働時間ブロックは、09:55:00–09:59:59の時刻を持つ確定済み足を選びます。足の時刻はOpenTimeなので、この足はUTC 10:00の完了時に届きます。Flagが時間帯の結果をセッションごとに正確に1回の注文パルスへ変換します。
- パルスはチャネルの両境界と現在ポジションを保存します。Session Low < Session HighかつPosition = 0なら、保存した安値に買い指値を登録し、続いて保存した高値に売り指値を登録します。どちらもOrder Volume 1で、ShrinkPriceは無効です。
- 継続購読されたLevel1ブロックがBestBidを読み取ります。この気配ストリームにより、取引コネクターはリアルタイム価格更新を受け取り、相場が注文価格に達したときに2つの待機注文を約定できます。BestBidは保存したチャネル境界の価格を置き換えません。
- どちらかの指値注文で最初のMyTradeが発生すると、保存した反対側のOrderの取消を要求します。これはクライアント側のOCOロジックです。通常の逐次イベント処理では約定済みチャネル注文を1つにする想定ですが、取消は取引所でアトミックではないため、ほぼ同時の約定が競合する可能性があります。次のリセットでは一括取消要求を送り、保存済みの2つのOrder参照もそれぞれの確定的な取消経路へ渡し、Order Volume数量のReduceOnly成行処理を実行します。通常の1約定ケースでは全決済し、実際のエクスポージャーが異なる場合はReduceOnlyで縮小だけを行います。チャートには足、2本のチャネル線、2つの注文ストリーム、入場とリセット決済の全約定が表示されます。

## エントリーとエグジットの条件

- **ロングエントリー**: UTC 10:00のセッション境界で、108値の両インジケーターが形成済み、保存した安値が保存した高値より低く、ポジションのスナップショットがゼロなら、Session LowにOrder Volumeの買い指値を置きます。注文は約定するか取消経路で削除されるまで有効です。
- **ショートエントリー**: 同じ形成済みチャネルとフラットポジションの確認後、Session HighにOrder Volumeの売り指値を置きます。この注文が先に約定すると、MyTradeイベントが保存済み買い指値を取消ブロックへ渡します。
- **エグジット**: ストップロスとテイクプロフィットのブロックはありません。チャネル注文の最初の約定後、クライアント側ロジックは意図的なポジション反転を行わず、反対注文の取消を要求します。ポジションは00:55足に関連するリセットまで保持され、その足がUTC 01:00に完了した時点で処理されます。リセットは一括取消を要求し、保存済みの両指値を明示的に取り消してから、Order Volume数量のReduceOnly成行処理を使います。通常の1約定ポジションは全決済し、異なる実エクスポージャーを増加または反転させることはありません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles Series | 00:05:00 | 5分足系列。チャネルの更新と2つの日次時間帯の駆動には確定済み足だけを使います。 |
| Session High Length | 108 | 形成済みHighestが使う確定済み足の本数です。既定の時間枠と時間帯では、108本がUTC 01:00–10:00を覆います。 |
| Session High Source | unset | 未設定です。Highestは各確定済み足のHighを自動的に読み取ります。 |
| Session Low Length | 108 | 形成済みLowestが使う確定済み足の本数です。両境界が同じセッションを表すよう、Session High Lengthと同じ値にします。 |
| Session Low Source | unset | 未設定です。Lowestは各確定済み足のLowを自動的に読み取ります。 |
| Order Volume | 1 | 各待機指値とリセット時のReduceOnly処理に使う数量です。通常の1約定経路では発生したポジション数量と一致し、実際の数量が異なる場合もReduceOnlyによってリセット時の増加や反転を防ぎます。 |

## ダイアグラムの詳細

- [ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは確定済み5分足だけを出力します。形成済み値だけを出力する2つの[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックが、そのストリームから移動Highest 108とLowest 108を直接計算します。
- 2つの[稼働時間](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)ブロックが足のOpenTimeを確認します。09:55:00–09:59:59の注文区間は足がUTC 10:00に完了したとき、00:55:00–00:59:59の区間はUTC 01:00に完了したときに動作します。この1本分のずれは時間設定の一部であり、執行遅延ではありません。
- 注文用[Flag](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/flag.html)は1回だけ出力し、リセットまで設定状態を保ちます。[変数](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)ブロックがそのパルスでHighest、Lowest、Position、ゼロ、数量を保存し、[比較](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)と[論理条件](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)が、有効なチャネルかつフラットポジションで1組だけを許可します。
- 買いの[注文登録](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/orders/register.html)ブロックが最初にSession Lowへ指値を置きます。その登録済みOrderが高値と数量の入力を更新してから、Session Highの売り登録を起動します。両注文はShrinkPrice=falseで、約定または取消まで有効です。
- [Level1](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html)ブロックはBestBidを継続購読します。保持されたストリームが待機指値の約定に必要なリアルタイム気配処理を有効にし、注文価格自体は保存されたHighestとLowestだけから取得します。
- 登録された各Orderは保存されます。買いMyTradeは保存済み売り注文を[注文取消](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html)へ渡し、売りMyTradeは買い注文に対して対称に動作します。リセットは[一括注文取消](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html)を呼び出し、保存済みの両Orderもそれぞれ対応する取消ブロックへ渡すため、クリーンアップは一括要求の確認に依存しません。
- 最後に、リセットパルスがReduceOnly条件、Order Volume数量、MarketOrderアルゴリズムで[ポジション変更](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)を起動します。想定する逐次処理の1約定経路ではこの数量で全決済し、約定が競合した場合や実エクスポージャーが異なる場合もReduceOnlyによって増加や反転を防ぎます。[チャートパネル](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/chart.html)は、確定済み足、Highest、Lowest、2つのOrderストリーム、2つの指値MyTradeストリーム、リセット決済MyTradeストリームを受け取ります。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
