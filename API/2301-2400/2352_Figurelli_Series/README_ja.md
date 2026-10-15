# Figurelli Series戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略はMetaTrader5のエキスパート「Exp_FigurelliSeries」をStockSharpに変換したものです。現在の価格より上下にある移動平均の数の差を測定するカスタムFigurelli Seriesインジケーターを使用します。取引はユーザー定義の開始時刻に1日1回発生し、すべてのポジションは停止時刻にクローズされます。

## インジケーター
Figurelli Seriesインジケーターは*Start Period*から始まり、*Total*本の平均線について*Step*ずつ増加する指数移動平均のチェーンを作成します。各バーで、終値より上下にある平均線の数を数えます。インジケーターの値は`bids - asks`であり、`bids`は価格より下の平均線の数、`asks`は価格より上の平均線の数です。

## 取引ルール
- `Start Hour:Start Minute`の時点で:
  - インジケーター値が正でロングポジションがない場合は買い。
  - インジケーター値が負でショートポジションがない場合は売り。
- `Stop Hour:Stop Minute`以降は、開いているポジションはすべてクローズされます。
- 選択した`Candle Type`の完成したローソク足のみが使用されます。

## パラメーター
- `StartPeriod` – 移動平均の初期期間。
- `Step` – 平均線間の期間増分。
- `Total` – 移動平均の数。
- `StartHour` / `StartMinute` – エントリーが可能な時刻。
- `StopHour` / `StopMinute` – すべてのポジションを閉じる時刻。
- `CandleType` – 計算に使用するローソク足の種類。
