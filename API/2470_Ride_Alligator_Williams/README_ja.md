# Ride Alligator Williams戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はBill WilliamsのAlligatorインジケーターを実装します。Lips、Teeth、Jawの各ラインは、黄金比によって基本期間から導出された長さを持つスムーズ移動平均を使用して中間価格から計算されます。TeethがJawより下にある状態でLipsがJawを上抜けた時にロングポジションを開きます。TeethがJawより上にある状態でLipsがJawを下抜けた時にショートポジションを開きます。オープンポジションに対してはトレーリングストップがJawラインに沿って追従します。

## パラメーター
- **Base Period** – Alligatorの長さを導出するためのルート期間。
- **Candle Type** – 入力ローソク足の時間軸。

## インジケーター
- スムーズ移動平均（Lips、Teeth、Jaw）

## エントリールール
- LipsがJawを上抜けてTeethがJawより下にある時にロング。
- LipsがJawを下抜けてTeethがJawより上にある時にショート。

## エグジットルール
- 逆方向のクロスオーバーでポジションを決済。
- JawラインのトレーリングストップはJawラインを価格が交差した時に決済。
