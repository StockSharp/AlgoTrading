# セッションレンジ突破戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、UTC の日中セッションで BTCUSDT の移動する8時間レンジの突破を取引します。確定済み1時間足が水準と判断を定め、共有の日次 Flag がその日の最初の方向候補だけを通し、夜間の専用経路がダイアグラムで建てたポジションをゼロへ戻します。

![schema](schema.svg)

## 戦略の概要

- 確定済み1時間足は、形成済みの値だけを出力する Highest 8 と Lowest 8 に入る前に1期間シフトされます。そのため、新しい足ごとの2水準は直前の完了済み8時間を表し、1日中固定されず継続的に移動します。
- 共通の Time クロックが UTC 00:00:00～07:59:59 に日次リセット経路を有効にします。足の開始時刻は、08:00:00～19:59:59 の取引時間帯と20:00:00～23:59:59 の決済時間帯を別々に駆動します。これらの設定は半開区間 `[08:00, 20:00)` と `[20:00, 24:00)` を実現します。
- 取引時間帯では、厳密な `Close > High` と `Position <= 0` がロング候補を作り、厳密な `Close < Low` と `Position >= 0` がショート候補を作ります。終値がどちらかのレンジ境界と等しい場合はエントリーしません。
- 両方向の候補は1つの Flag を共有するため、UTC の各日で最初の適格なロングまたはショート候補だけがエントリーできます。エントリー数量は `Base Volume + abs(Position)` で、フラットから1単位を建てるか、反対側の1単位ポジションを1本の成行注文で決済して反転します。
- 決済時間帯では、正のポジションが基本数量の成行売りを、負のポジションが基本数量の成行買いを送ります。ストップロスとテイクプロフィットのブロックはなく、チャートには足、移動する Highest と Lowest、水準、および4本の MyTrade ストリームが表示されます。

## エントリーとエグジットの条件

- **ロングエントリー**: UTC 08:00:00～19:59:59 に、確定済み足が直前8時間に対して `Close > Highest(8)` を満たし、ポジションのスナップショットが `<= 0` で、共有の日次 Flag が利用可能なら、ダイアグラムは数量 `1 + abs(Position)` の NoCondition 成行買いを送ります。
- **ショートエントリー**: UTC 08:00:00～19:59:59 に、確定済み足が直前8時間に対して `Close < Lowest(8)` を満たし、ポジションのスナップショットが `>= 0` で、共有の日次 Flag が利用可能なら、ダイアグラムは数量 `1 + abs(Position)` の NoCondition 成行売りを送ります。
- **エグジット**: UTC 20:00:00～23:59:59 に、ポジションが正なら Base Volume 1 を売り、負なら Base Volume 1 を買います。通常動作ではエントリーが正確に `+1` または `-1` のエクスポージャーを作るため、固定の決済数量でゼロへ戻ります。ストップロス、テイクプロフィット、その他の保護は接続されていません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 01:00:00 | BTCUSDT の1時間時間枠です。移動レンジ、セッション判定、ポジションのスナップショット、売買判断には確定済み足だけを使います。 |
| Range Length | 8 | 形成済みの値だけを出力する Highest と Lowest が使う、シフト済み確定足の本数です。現在の足は計算から除外されます。 |
| Reset Window | 00:00:00–07:59:59 UTC | 取引セッション前に、共通の Time クロックが共有の日次 Flag をリセットする UTC 区間です。 |
| Trade Window | 08:00:00–19:59:59 UTC | エントリー候補に対する UTC の包含設定境界です。1時間足の開始時刻では半開区間 `[08:00, 20:00)` に相当します。 |
| Close Window | 20:00:00–23:59:59 UTC | ポジションをフラットにする UTC の包含設定境界です。1時間足の開始時刻では半開区間 `[20:00, 24:00)` に相当します。 |
| Base Volume | 1 | フラットからのエントリーで使う1単位の数量です。反転時には `abs(Position)` に加算され、夜間の2本の決済注文にはそのまま渡されます。 |

## ダイアグラムの詳細

- [ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは BTCUSDT の確定済み1時間足を出力します。Shift 1 の[前の値](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)ブロックが、判断対象の足をレンジ計算から除外します。
- 形成済みの値だけを出力する2つの[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックが、シフト済み足ストリームから Highest 8 と Lowest 8 を計算します。出力は1時間が完了するたびに更新され、直前8時間の移動チャネルを表します。
- 共通の Time ストリームが UTC 00:00:00～07:59:59 のリセット用[稼働時間](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)ブロックを駆動します。取引用と決済用の稼働時間ブロックは足ストリームが直接駆動するため、各足の OpenTime を判断に使います。
- 各判断足で現在の[ポジション](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/current.html)を取得します。[比較](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)と[論理条件](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)のブロックが、厳密な突破、セッション、ポジション方向、共有 Flag の判定を組み合わせます。
- エントリー数量の演算は `Base Volume + abs(Position)` を計算します。2つのエントリー用[ポジション変更](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ブロックは NoCondition 成行注文を出し、フラットからロングまたはショートを1単位建てるか、反対側の1単位ポジションを1本の注文で完全に反転します。
- リセット時間帯が UTC の1日について共有 Flag を1つ復帰させ、最初に受理されたロングまたはショート候補がそれを消費します。決済時間帯では、正と負のポジションに分かれた経路が固定 Base Volume 1 の成行注文を送り、通常のエントリー経路で作られた `±1` のエクスポージャーを閉じます。
- [チャートパネル](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/chart.html)は、確定済み足、Highest 8、Lowest 8、およびロングエントリー、ショートエントリー、ロング決済、ショート決済の MyTrade 出力を受け取ります。ストップロスまたはテイクプロフィットの要素はありません。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
