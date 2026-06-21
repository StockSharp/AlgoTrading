# Tester v0.14 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このサンプル戦略は、EURUSD のH4時間軸向けに設計されたMQL4スクリプト「Tester v0.14」を簡略化した移植版です。

## ロジック

- 14期間の単純移動平均線とMACDを計算する。
- 終値がSMAを上回り、MACDがプラスの場合に買いシグナルを生成する。
- 終値がSMAを下回り、MACDがマイナスの場合に売りシグナルを生成する。
- 注文が開かれた後、設定可能な本数のバー後にポジションをクローズする。

この移植版はStockSharpの高レベルAPIを使用し、`SubscribeCandles` と `Bind` に依存してインジケーター値を受け取ります。

## パラメーター

- **MinSignSum** – ポジションを開くために必要な最小シグナル数。
- **Risk** – マネーマネジメントに使用する口座残高のパーセンテージ。
- **TakeProfit / StopLoss** – ポイント単位の固定レベル。
- **BarsNumber** – ポジションを保持するバー数。
- **CandleType** – 使用するローソク足シリーズ（デフォルト: 4H）。

## 注記

元のMQLファイルには数百のルールの組み合わせが含まれていました。このC#の例は、わかりやすさのために縮小されたルールセットを使って構造を示しています。
