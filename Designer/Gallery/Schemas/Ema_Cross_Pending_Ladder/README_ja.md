# EMAクロス指値ラダー戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、確定したEMAクロスを2段階のエントリーで取引します。成行注文でポジションを新規作成または完全反転し、その全約定後に最新のBestBidを基準として同方向の離れた指値注文を置きます。絶対価格幅のポジション保護がエクスポージャーを管理します。100本のクールダウン中は、新しい第1段階エントリーと確定済み終値による保護価格判定の両方が停止します。

![schema](schema.svg)

## 戦略の概要

- 確定済み5分足が、形成済み値だけを出力する高速EMA 14と低速EMA 50に入ります。高速EMAが低速EMAを上抜けるとCrossingは上方向イベントを、下抜けると下方向イベントを生成します。
- 足時点のポジションスナップショットと準備状態がエントリーを制限します。上抜けではPosition <= 0のときだけ買い、下抜けではPosition >= 0のときだけ売ります。第1段階の成行数量はBase Volume + abs(Position)なので、フラットから建てるか反対ポジションを完全に反転します。
- 第1段階の成行注文がMatchedになると、継続取得している最新BestBidを第2段階の基準にします。ロング側はBestBid - 100で買い指値、ショート側はBestBid + 100で売り指値を出し、どちらもBase Volume 1、ShrinkPrice無効です。
- 最後に登録された第2段階注文は、エントリー制限を通さない反対方向のEMAクロス、保護約定、またはクールダウン完了で取り消されます。第2段階の約定はポジション保護に加わりますが、クールダウンは再開しません。
- 4つのエントリー注文ブロックの約定がすべて、Take Distance 400とStop Distance 200の絶対価格幅による保護に入ります。準備状態が有効な間は確定済み終値ごとに判定し、発動した決済を成行で送ります。第1段階の約定または保護決済でクールダウンが始まり、次の確定済み100本では新しい第1段階エントリーも保護価格判定も行わず、101本目に両方を再開します。チャートには足、2本のEMA、2つの指値Orderストリーム、5つの約定ストリームが表示されます。

## エントリーとエグジットの条件

- **ロングエントリー**: 高速EMAが低速EMAを上抜け、ポジションスナップショットがゼロ以下でクールダウンが準備済みなら、Base Volume + abs(Position)を成行買いします。その注文が全約定すると、保存したBestBidからRung Distanceを引いた価格にBase Volumeの買い指値を置きます。
- **ショートエントリー**: 高速EMAが低速EMAを下抜け、ポジションスナップショットがゼロ以上でクールダウンが準備済みなら、Base Volume + abs(Position)を成行売りします。その注文が全約定すると、保存したBestBidにRung Distanceを加えた価格にBase Volumeの売り指値を置きます。
- **エグジット**: ポジション保護は2つの成行段階と2つの指値段階の約定を受け取ります。準備状態が有効な間は確定済み足の終値を判定し、有利方向400価格単位または不利方向200価格単位に到達すると成行決済します。第1段階の約定または保護決済後の確定済み100本では保護価格判定を行わず、101本目に再開します。保護約定は最後の待機中の第2段階を取り消し、制限前の反対クロスもエントリー準備状態に関係なくその注文を取り消します。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 00:05:00 | 5分時間枠。EMA計算、シグナル、保護判定、クールダウンの計数は確定済み足だけで進みます。 |
| Fast EMA Length | 14 | 高速ExponentialMovingAverageの長さ。形成済み値だけを出力します。 |
| Slow EMA Length | 50 | 低速ExponentialMovingAverageの長さ。形成済み値だけを出力します。 |
| Base Volume | 1 | 第1段階の成行数量ではabs(Position)に加算し、第2段階の指値数量ではそのまま使用する数量です。 |
| Rung Distance | 100 price units | 保存したBestBidからの絶対価格オフセット。買い指値では減算し、売り指値では加算します。 |
| Cooldown | 100 candles | 新しい第1段階エントリーと保護価格判定の両方を停止する後続の確定済み足の本数。101本目に両方を再開します。 |
| Take Distance | 400 price units | 保護成行決済を起動する有利方向の絶対価格変動です。 |
| Stop Distance | 200 price units | 保護成行決済を起動する不利方向の絶対価格変動です。 |

## ダイアグラムの詳細

- [ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは確定済み5分足だけを出力します。形成済み値に限定した2つの[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックがExponentialMovingAverage 14と50を計算します。
- [クロス](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)は上方向イベントでtrue、下方向イベントでfalseを出力し、NOTの[論理条件](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)が下方向イベントを操作可能にします。[ポジション](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/current.html)はEMA経路より前に取得され、[比較](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)ブロックがPosition <= 0またはPosition >= 0を準備状態と組み合わせます。LongとShortのエントリーゲートはtrueパルスだけを第1段階トリガーへ渡します。
- [フォーミュラ](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/formula.html)がBase Volume + abs(Position)を計算します。第1段階の[注文登録](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/orders/register.html)ブロックがNoCondition成行注文を送り、そのMatched出力が対応する第2段階を起動します。
- 継続動作する[Level1](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html)ブロックがBestBidを提供し、[変数](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)が保持します。価格[フォーミュラ](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/formula.html)ブロックがBestBid - Rung DistanceとBestBid + Rung Distanceを計算し、第2段階の[注文登録](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/orders/register.html)ブロックがBase Volume、ShrinkPrice falseの同方向指値注文を出します。
- 新しい第2段階は、その都度[注文取消](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html)用に保持される注文になります。反対側への直接クロス、保護約定、またはクールダウン完了が取消を起動します。指値段階の約定は保護対象のエクスポージャーに加わりますが、クールダウンは起動しません。
- [ポジション保護](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)は4つのエントリーブロックの約定を受け取り、絶対テイク幅とストップ幅を使用して成行決済を送ります。保存された確定済み終値は、準備状態が有効なときだけ価格判定へ渡されます。[N個の値](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)ブロックと状態変数は、第1段階の成行約定または保護決済の後、続く確定済み100本で第1段階エントリーとこれらの判定の両方を停止し、101本目に両方を再開します。
- [チャートパネル](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/chart.html)は、確定済み足、高速EMA 14、低速EMA 50、買い指値と売り指値のOrderストリーム、および成行買い、成行売り、指値買い、指値売り、保護決済の5つのMyTradeストリームを受け取ります。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
