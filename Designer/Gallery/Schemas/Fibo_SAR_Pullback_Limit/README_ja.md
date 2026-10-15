# フィボナッチ SAR 押し目指値戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは速度の異なる二つの Parabolic SAR と3本のローソク足レンジを組み合わせ、フィボナッチ押し目の指値注文を同時に一つだけ出し、条件が反転した未約定注文を取り消し、エントリー時に保存したレンジ由来の水準で約定ポジションを決済します。

![schema](schema.svg)

## 戦略の概要

- 確定した1時間足 BTCUSDT ローソク足を、高速・低速 Parabolic SAR、Highest(3)、Lowest(3) に入力します。すべての指標が形成されてから判定を始めます。
- 形成済み Lowest の出力が、現在の Close、両 SAR、高値、安値、ポジション、未決済注文状態を保存した後に、1回の判定バッチを解放します。
- 共通の未決済ロックにより、有効なエントリー注文は同時に一つだけです。注文が最終状態になるとロックを解除し、約定後はサンプリングしたポジションが次のエントリーを防ぎます。
- エントリー、取消、決済の条件はサイレントなスコア保持部に保存され、確定足ごとに一度だけ解放されるため、隣接する足の値が混ざりません。
- チャートにはローソク足、両 SAR、レンジと保存済み保護水準、登録・取消済み指値注文、成行決済、全約定を表示します。

## エントリーと決済のルール

- **ロングエントリー**: `Slow SAR < Fast SAR < Close`、ポジションがフラット、待機中のエントリーがない場合、`Low3 + (High3 - Low3) * 50%` に Buy 指値注文を出します。約定前に `Slow SAR > Fast SAR` または `Fast SAR >= Close` となれば取り消します。
- **ショートエントリー**: `Slow SAR > Fast SAR > Close`、ポジションがフラット、待機中のエントリーがない場合、`High3 - (High3 - Low3) * 50%` に Sell 指値注文を出します。約定前に `Slow SAR < Fast SAR` または `Fast SAR <= Close` となれば取り消します。
- **決済**: エントリー信号を受理した時点で方向別の水準を保存します。ロングのストップは `Low3 - 30`、目標は `Low3 + (High3 - Low3) * 161%`、ショートのストップは `High3 + 30`、目標は `High3 - (High3 - Low3) * 161%` です。確定 Close が保存水準のいずれかに達すると、数量1の反対方向の成行注文を一つ出します。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Security | BTCUSDT@BNBFT | 確定足購読で使う銘柄です。注文と約定のため Strategy Security にも同じ銘柄を設定します。 |
| Candle Series | 01:00:00 | 指標、判定、決済、チャートに使う確定1時間足です。 |
| Fast SAR Acceleration | 0.02 | 高速 Parabolic SAR の初期加速係数です。 |
| Fast SAR Increment | 0.02 | 高速 Parabolic SAR の加速増分です。 |
| Fast SAR Maximum | 0.20 | 高速 Parabolic SAR の最大加速係数です。 |
| Slow SAR Acceleration | 0.01 | 低速 Parabolic SAR の初期加速係数です。 |
| Slow SAR Increment | 0.02 | 低速 Parabolic SAR の加速増分です。 |
| Slow SAR Maximum | 0.10 | 低速 Parabolic SAR の最大加速係数です。 |
| High Lookback | 3 | Highest が `High3` の計算に使う確定足の本数です。 |
| Low Lookback | 3 | Lowest が `Low3` の計算に使う確定足の本数です。 |
| Entry Fibonacci, % | 50 | 現在の3本レンジ内に置く指値価格の位置です。 |
| Target Fibonacci, % | 161 | 保存する各利益目標に使うレンジ倍率です。 |
| Stop Offset | 30 | 保存ストップを3本レンジの安値または高値の外側に置く絶対価格距離です。 |
| Order Volume | 1 | 各エントリーと方向ガード付き成行決済の数量です。 |

## ダイアグラムの詳細

- Security [Variable](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) が確定 [Candles](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) を設定し、取引ブロックは Strategy Security と Strategy Portfolio を使用します。
- 形成済みの値だけを出力する四つの [Indicator](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) が、両 Parabolic SAR と3本の高値・安値を個別に計算します。Lowest の出力が共通バッチクロックです。
- [Formula](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/formula.html)、Variable、[Comparison](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) が数値入力を揃え、フラット状態と未決済方向のガードを適用し、真のアクションパルスだけを出します。
- 受理された各信号は、[Order registering](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/orders/register.html) を起動する前に計算済みのストップと目標を保存します。ポジション保有中、保存値は動きません。
- 登録済み Order の参照を、対象指定の [Order cancellation](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html) 用に保持します。未決済ロックは、約定、確認済み取消、登録失敗の後に登録ブロックが出す Finished イベントでのみ解除されます。
- 方向ガード付き [Modify position](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) は、確定 Close が保存済みストップまたは目標に達したとき、固定1単位の反対成行注文を出します。チャートは関連する価格、注文、取消、MyTrade の全ストリームを受け取ります。

## 使用方法

`.json` ファイルを Designer に読み込み、Strategy Security を BTCUSDT@BNBFT に設定して1時間足履歴で実行します。実運用の前に、銘柄の価格スケール、フィボナッチ水準、ストップ幅、注文ライフサイクル、成行決済の動作を確認してください。
