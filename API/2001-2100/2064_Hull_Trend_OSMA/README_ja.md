# Hull Trend OSMA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は MetaTrader の "Exp_HullTrendOSMA" エキスパートアドバイザーの変換版です。

## 概要

この戦略は Hull Trend OSMA インジケーターを使用します。このインジケーターは Hull Moving Average とその平滑化バージョンを計算します。オシレーター値はこれら 2 つのシリーズの差です。オシレーターが連続する 2 本の完成した足で上昇すると、戦略はロングポジションを開きます。オシレーターが連続する 2 本の完成した足で下落すると、戦略はショートポジションを開きます。各シグナルで反対のポジションがクローズされます。

## パラメーター

- **Hull Period** – Hull Moving Average の期間。
- **Signal Period** – オシレーターに適用する平滑化移動平均の期間。
- **Take Profit** – 価格単位でのテイクプロフィット注文の距離。
- **Stop Loss** – 価格単位でのストップロス注文の距離。
- **Candle Type** – 計算に使用する足の時間軸（デフォルト 8 時間）。

## 注意事項

- 自動足サブスクリプション付きの StockSharp 高レベル API を使用します。
- エントリーとエグジットは成行注文で実行されます。
- ストップロスとテイクプロフィットの保護は戦略開始時に一度初期化されます。
