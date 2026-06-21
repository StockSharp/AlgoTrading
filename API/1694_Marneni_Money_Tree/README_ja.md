# Marneni Money Tree戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MQLエキスパートアドバイザー「Marneni Money Tree」をStockSharpに移植したものです。
40期間の単純移動平均（SMA）と2つのシフトした値を使用してトレンドの方向を検出します。
4本前のバーでシフトしたSMAが現在のSMAと30本前の値の間にある場合、
- 検出された方向に成行注文が送信されます；
- `Order2Pips`から`Order9Pips`で定義された増加する距離に8つの追加の指値注文が置かれます。

ロング設定では現在価格より下に買い指値が置かれます。ショート設定では価格より上に売り指値が置かれます。
SMAの関係が逆転すると、ポジションが決済され残りの注文がキャンセルされます。

## パラメーター
- `Order2Pips`–`Order9Pips` — 指値注文2から9のpips単位の距離。
- `CandleType` — 計算に使用する時間軸。

基本取引量は2に固定されており、戦略を開始する前に`Volume`プロパティを変更することで調整できます。
