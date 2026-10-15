# 24時間に1回の取引ストラテジー図
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、確定した4時間足における EMA(10) と EMA(30) の厳密なクロスを逆方向に取引します。設定可能な取引時間ゲートが候補の有効時間を制御し、Flag と6本を数える N values が、ローリング24時間ごとにエントリー判断を最大1回に制限します。

![schema](schema.svg)

## 戦略の概要

- 確定した4時間足を EMA 10 と EMA 30 に入力します。クロス状態は開始直後から更新されますが、エントリー候補が有効になるのは確定足10本の後です。
- 高速 EMA が低速 EMA を厳密に上抜くと売り候補が生じ、厳密に下抜くと買い候補が生じます。
- Time と Working time は設定区間内の候補だけを通します。Combination が2方向の候補ストリームをまとめ、Flag はリセットまで最初の有効候補だけを出力します。
- 許可されたエントリーが N values を開始します。その後、確定4時間足が6本進むとカウンターが Flag をリセットし、ローリング24時間の制限になります。
- ポジションがゼロなら Position modify は固定 Volume の成行注文を1件送ります。反対側の単位ポジションがある場合は、最初に決済し、2件目の固定 Volume の成行注文で新しい側を建てます。
- Position protection がエグジットを担当します。エントリーと反転の直接約定を追跡し、3%の利確または2%の固定損切りでポジションを決済できます。

## エントリーとエグジットの条件

- **ロングエントリー**: EMA の厳密な下向きクロス、確定足10本のウォームアップ、開いている Working time ゲート、利用可能なローリングラッチがそろうと Volume を買います。ゼロからはロングを建て、単位ショートからは1回目の買いで決済し、2回目でロングを建てます。
- **ショートエントリー**: EMA の厳密な上向きクロス、確定足10本のウォームアップ、開いている Working time ゲート、利用可能なローリングラッチがそろうと Volume を売ります。ゼロからはショートを建て、単位ロングからは1回目の売りで決済し、2回目でショートを建てます。
- **エグジット**: Position protection は追跡対象の建玉を3%の利確または2%の固定損切りで決済します。その後の有効な反対クロスは、決済してから新規に建てることでポジションを反転できます。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 04:00:00 | 4時間足です。EMA、ウォームアップ、ローリングカウンター、保護価格の確認は確定足だけで行います。 |
| Fast EMA Length | 10 | 高速指数移動平均の期間です。 |
| Slow EMA Length | 30 | 低速指数移動平均の期間です。 |
| Warmup Bars | 10 | クロスによるエントリーを許可するまでに必要な確定足数です。 |
| Session From | 00:00:00 | ストラテジーのリプレイ時間またはサーバー時間における有効セッションの開始です。 |
| Session Until | 23:59:59 | ストラテジーのリプレイ時間またはサーバー時間における有効セッションの終了です。 |
| Rolling Cooldown Bars | 6 | 許可されたエントリー後、次の判断を許可するまでに数える確定足数です。4時間足6本は24時間です。 |
| Volume | 1 | 各オープンまたは反転アクションに使う固定数量です。 |
| Take Profit % | 3 | Position protection が使う有利方向の変化率です。 |
| Stop Loss % | 2 | 固定・非トレーリング損切りが使う不利方向の変化率です。 |

## ダイアグラムの詳細

- [ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは、確定した4時間足を2つの[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックへ送ります。Crossing は EMA 10 と EMA 30 の関係変化を検出し、前回値と現在値の厳密な比較が各イベントを確認し、NOT 分岐が下向きクロスのパルスを作ります。
- 10本のウォームアップゲートが早期候補を阻止します。[Time](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/time.html) はリプレイ時間またはサーバー時間を [Working time](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/working_time.html) へ渡します。同梱履歴のリプレイは UTC です。
- Combination は実行可能な買い・売り候補を [Flag](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/flag.html) へ渡します。最初の true 候補が出力されて [N values](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/n_values.html) を開始し、その後の候補は確定足6本が追加されて Flag がリセットされるまで阻止されます。
- 現在ポジションが [Position modify](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) の経路を選びます。ゼロからのエントリーは固定方向の成行アクションを1回使い、反転は同じ方向のアクションを2回続け、最初にゼロへ戻して次に新規建てします。このため制限対象は許可されたエントリー判断であり、1回の判断が意図的に2回の約定を生むことがあります。
- オープンと反転のすべての直接約定が [Position protection](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) を更新します。確定足の終値が価格確認を動かし、保護自身の決済約定は取引入力へ戻しません。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
