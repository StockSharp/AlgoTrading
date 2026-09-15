# Open Drive 戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは 1 本のインパルス・ローソク足を捉えます。実体が現在の Average True Range の一定割合より大きいローソク足です。その実体の色が売買方向を決め、SMA 20 がそれと一致していなければならず、時刻は UTC の 1 日の最初の 6 時間の内側にあり、ポジションはフラットである必要があります。決済はテイクプロフィットとストップロスだけです。

![schema](schema.svg)

## 戦略の概要

- 確定した 5 分足が 2 つのコンバーター (Converter) を通じて終値と始値を供給し、SMA 20 と ATR 14 を駆動します。どちらのインジケーターも formed-only（形成後のみ）なので、それぞれが十分なローソク足を集めるまで、いかなる比較も判定を出しません。
- 1 つ目の数式 (Formula) は現在のローソク足の実体を `abs(Close - Open)` として測り、2 つ目は現在の ATR をしきい値 `ATR x 0.3` に変換します。比較 (Comparison) は実体がそのしきい値より厳密に大きいときにそのローソク足をインパルスと判定するため、ダイアグラムが動くバーはその時点のボラティリティに対して異例に大きなものになります。
- 2 つの比較が同じローソク足の色を `Close > Open` と `Close < Open` で読み取り、さらに 2 つが SMA 20 に対する位置を `Close > SMA` と `Close < SMA` で読み取ります。インパルスだけでは決して取引しません。色とトレンドが同じ方向を指している必要があります。
- 現在時刻 (Current time) ブロックが戦略時刻を、00:00:00 から 06:00:00 UTC をカバーする稼働時間 (Working time) ブロックへ流し込みます。その true/false の答えは変数 (Variable) に保存され、ローソク足が到着したときに再発行されます。これによりセッション・フィルターは独自のタイミングではなく、すべての価格比較と同じティックで判定されます。
- 変数はローソク足ごとにポジションのスナップショットを取り、ゼロとの比較がダイアグラムはフラットかどうかを示します。スナップショット経由でポジションを読むことで、2 本のローソク足の間に発生した約定がバーの途中でエントリー・ロジックを再び開くことを防ぎます。
- ロングの論理条件 (Logical condition) は `インパルス AND 陽線の実体 AND 終値が SMA より上 AND 時間帯の内側 AND フラット` であり、ショートはその鏡像です。いずれも 5 つの入力すべてを待つため、確定したローソク足 1 本につき判定をちょうど 1 回だけ発行します。
- true の判定は OpenPosition モードのポジション変更 (Position modify) ブロックを起動し、設定された数量の成行注文を送信します。ポジションが実際にゼロでなければ発注は拒否されます。したがって 1 本のローソク足が 2 つの取引を開くことはなく、保有中のポジションは新規エントリーを完全にブロックします。
- 両サイドのエントリー約定は組み合わせ (Combination) を通ってポジション保護 (Position protection) に入り、以後の確定した各ローソク足の終値に対して 3% のテイクプロフィットと 2% のストップロスを稼働させます。

## エントリーとエグジットの条件

- **ロングエントリー**: 00:00:00-06:00:00 UTC の内側で、SMA 20 と ATR 14 が形成済み、かつポジションがフラットのとき: `abs(Close - Open) > ATR x 0.3`、`Close > Open`、`Close > SMA 20` が成立すると、1 単位の OpenPosition 成行買い注文を送信します。
- **ショートエントリー**: 00:00:00-06:00:00 UTC の内側で、SMA 20 と ATR 14 が形成済み、かつポジションがフラットのとき: `abs(Close - Open) > ATR x 0.3`、`Close < Open`、`Close < SMA 20` が成立すると、1 単位の OpenPosition 成行売り注文を送信します。
- **エグジット**: シグナルによる決済も、ドテンもありません。ポジション保護 (Position protection) が、エントリー約定価格から 3% の利益または 2% の損失で取引を閉じます。判定は確定した各ローソク足の終値で行われるため、バー内で水準を突き抜けたスパイクは、そのローソク足が確定するまで実行されません。ダイアグラムは取引と取引の間にクールダウン・カウンターを持ちません。ポジションが閉じられれば、時間帯の内側で条件を満たす次のローソク足がすぐに別のポジションを開く可能性があります。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 00:05:00 | 確定したローソク足のタイムフレーム。すべての比較、両方のインジケーター、および保護水準はその終値で評価されます。 |
| MA Period | 20 | ローソク足がトレンドのどちら側で引けたかを決める、formed-only の単純移動平均の期間。 |
| ATR Period | 14 | その時点の通常のローソク足の大きさを表す、formed-only の Average True Range の期間。 |
| ATR Multiplier | 0.3 | インパルスと見なされるためにローソク足の実体が超えなければならない、現在の ATR に対する割合。大きくするとより稀で大きなローソク足を要求し、小さくすると普通のローソク足も受け入れます。 |
| Window Begin | 00:00:00 | 取引時間帯の開始 (UTC)。それより前はインパルスは計測・描画されますが、決して取引されません。 |
| Window End | 06:00:00 | 取引時間帯の終了 (UTC)。この組を 00:00:00-23:59:59 に広げれば、ダイアグラムは 24 時間取引できます。 |
| Order Volume | 1 | 両方のエントリーが送信する数量。保有中は 2 回目のエントリーが拒否されるため、ポジションは常に 1 単位です。 |
| Take Profit | 3% | エントリー約定価格に対する百分率でのテイクプロフィット幅。 |
| Stop Loss | 2% | エントリー約定価格に対する百分率でのストップロス幅。 |

## ダイアグラムの詳細

- [ローソク足 (Candles)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) ブロックは確定した 5 分足を発行し、同梱の 1 分足ヒストリーからそれを組み立てられます。2 つの [コンバーター (Converter)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) が終値と始値を読み取り、formed-only の 2 つの [インジケーター (Indicator)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) ブロックが SMA 20 と ATR 14 を計算します。
- 2 つの [数式 (Formula)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) ブロックが実体と ATR しきい値を組み立て、5 つの [比較 (Comparison)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) ブロックが実体、色、トレンドに対する位置、ポジションをシグナルに変えます。
- [現在時刻 (Current time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) ブロックが戦略時刻を [稼働時間 (Working time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) に送り込みますが、その答えはローソク足よりもはるかに頻繁に変化します。Input as trigger をオフにした [変数 (Variable)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) がその答えを保存し、次のローソク足がトリガーしたときにだけ解放します。
- [ポジション (Position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) は 2 つ目の変数でスナップショットが取られ、定数ゼロと比較されます。5 入力の [論理条件 (Logical condition)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) ブロックはどちらもすべての入力を待つため、各ローソク足はロング判定 1 つとショート判定 1 つを生みます。
- 2 つの OpenPosition [ポジション変更 (Modify position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) ブロックが成行で発注します。[組み合わせ (Combination)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) が両方のエントリー約定を [ポジション保護 (Position protection)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) のためにまとめ、[チャートパネル (Chart panel)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) がローソク足、SMA、ATR、保護用の一対を含むすべての注文、そしてすべての約定を描画します。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
