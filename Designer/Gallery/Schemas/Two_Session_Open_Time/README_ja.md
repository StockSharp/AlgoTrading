# 2つのセッション開始時刻戦略のダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムにはインジケーターが一切ありません。シグナルの源は時計だけです。取引日の2つの別々の時間帯がそれぞれ1回ずつ買いポジションを建て、フラグ (Flag) が各時間帯のエントリーを1日1回に制限し、3つ目の時間帯が残っているポジションをすべて手仕舞いして、翌日に向けて両方の時間帯を再び有効にします。

![schema](schema.svg)

## 戦略の概要

- 時刻 (Time) が現在時刻を3つの稼働時間 (Working time) ブロックへ流し込みます。エントリー用の2つの時間帯は 09:30-14:00 と 00:00-04:00、強制手仕舞い用の時間帯は 19:50-20:00 です。
- 各エントリー時間帯は、And に設定された論理条件 (Logical condition) でノーポジション判定と結合されます。そのため時間帯がエントリーを要求できるのは、ポジションを何も持っていない間だけです。
- 時間帯は数時間続き、そのゲートは同じ true の値を出し続けます。フラグ (Flag) はゲートと注文の間に置かれ、その最初の1つだけを通すため、長い時間帯が1回のエントリーになります。
- どちらの時間帯も買いです。ポジション変更 (Position modify) は Open position 条件で動作するため、Order Volume の成行注文が出るのはポジションがちょうどゼロのときだけです。
- 強制手仕舞いの時間帯は、Close position に設定された3つ目のポジション変更を動かします。まったく同じシグナルが両方のフラグをリセットするので、2つのエントリー時間帯は翌日に向けて再び有効になります。
- ポジション保護 (Position protection) は両方のエントリーの約定を監視し、1.5% の利益確定、またはローソク足の終値に追随する 0.5% のトレーリングストップでポジションを閉じます。
- 確定した5分足がダイアグラム全体のテンポを決めます。終値をポジション保護へ渡し、パネルに描画されるのもこのローソク足であり、その到着が時計を進めます。
- チャートパネルには、ローソク足、保護が反応する価格ライン、ダイアグラムが送信したすべての注文と受け取ったすべての約定が表示されます。

## エントリーとエグジットの条件

- **ロングエントリー**: どちらの時間帯でも、ポジションがない間にその時間帯のフラグ (Flag) が最初の true シグナルを通し、ポジション変更 (Position modify) が Open position 条件で Order Volume を成行で買います。同じ時間帯のそれ以降のシグナルは、手仕舞いの時間帯がリセットするまでフラグに吸収されます。
- **ショートエントリー**: 売り側はありません。どちらの時間帯も買いで建て、ダイアグラムが出す売り注文は、建っている買いポジションを閉じるものだけです。
- **エグジット**: ポジション保護 (Position protection) は、1.5% の利益確定、またはローソク足の終値に追随する 0.5% のトレーリングストップでポジションを閉じます。手仕舞いの時間帯が始まった時点でまだ残っているものは Close position のアクションが手仕舞いし、同時に両方のラッチもクリアされます。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 00:05:00 | 5分の時間足。確定したローソク足だけが処理され、その終値が保護の価格判定とチャートのラインの元になります。 |
| First Window From | 09:30:00 | 最初のエントリー時間帯の開始時刻（再生時間またはサーバー時間）。 |
| First Window Until | 14:00:00 | 最初のエントリー時間帯の終了時刻。これ以降、この時間帯はエントリーを準備できません。 |
| Second Window From | 00:00:00 | 2つ目のエントリー時間帯の開始時刻（再生時間またはサーバー時間）。 |
| Second Window Until | 04:00:00 | 2つ目のエントリー時間帯の終了時刻。 |
| Close Window From | 19:50:00 | 強制手仕舞いの時間帯の開始時刻。建っているポジションを手仕舞いし、両方のラッチをリセットします。 |
| Close Window Until | 20:00:00 | 強制手仕舞いの時間帯の終了時刻。 |
| Order Volume | 1 | 両方の時間帯のエントリーで使う固定数量。 |
| Take Profit, % | 1.5 | ポジション保護がポジションを閉じる、有利方向への変動率。 |
| Stop Loss, % | 0.5 | ストップの不利方向への変動率。トレーリング方式により、価格が有利に動くとローソク足の終値に追随して引き上げられます。 |

## ダイアグラムの詳細

- [ローソク足 (Candles)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) ブロックは確定した5分足を出力します。[コンバーター (Converter)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) がその終値を取り出し、この価格が [ポジション保護 (Position protection)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) の利益確定とトレーリングストップの基準となり、またローソク足の横に描かれるラインになります。価格から計算されるものは他にありません。ダイアグラムはインジケーターを持ちません。
- [時刻 (Time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) は現在時刻を3つの [稼働時間 (Working time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) ブロックへ供給します。そのうち2つがエントリーの時間帯を、1つが強制手仕舞いの時間帯を示します。同梱のヒストリーの再生は UTC で動作するため、時間帯の境界は UTC の時刻として解釈されます。
- [ポジション (Position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) をゼロの [変数 (Variable)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) と [比較 (Comparison)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) で突き合わせるとノーポジション判定が得られ、これを2つの [論理条件 (Logical condition)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) の And ゲートが共有します。そのため、ポジションを持っていると、もう一方の時間帯も黙って塞がれます。
- [フラグ (Flag)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) が、時間帯を1回のイベントに変えます。トリガーは And ゲート、リセットは手仕舞いの時間帯です。値を通すのはセットされた瞬間だけなので、4時間の時間帯が生む数百回の true が1回のエントリーにまとまります。
- [ポジション変更 (Position modify)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) ブロックは3つ動作します。Order Volume を使う Open position のエントリーが2つと、手仕舞いを行う Close position が1つで、後者は取り消すべきポジションを自ら読み取るため数量を必要としません。2つのエントリーの約定は [結合 (Combination)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) でまとめられてポジション保護へ渡されます。ポジション保護自身の手仕舞い約定はパネルに描かれますが、その約定の入力へは戻されません。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
