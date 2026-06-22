# デイトレード指標フュージョン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Parabolic SAR、MACD (12,26,9)、Stochastic Oscillator (5,3,3)、Momentum (14) を使用して5分足を取引します。ポジションに入る前にすべてのインジケーターが一致していることを必要とします。

- **ロングエントリー**: SARが価格より下にあり前のSARが現在より上、Momentum < 100、MACDラインがシグナルライン未満、Stochastic %K < 35。
- **ショートエントリー**: SARが価格より上にあり前のSARが現在より下、Momentum > 100、MACDラインがシグナルライン超過、Stochastic %K > 60。

反対の条件が発生するとポジションはクローズされます。リスク管理にはトレーリングストップとオプションのテイクプロフィットを使用します。

## パラメーター
- **Volume** – 注文数量。
- **Take Profit** – 目標利益（ポイント単位）。
- **Trailing Stop** – トレーリングストップの距離（ポイント単位）。
- **Candle Type** – 足のサブスクリプションタイプ（デフォルト：5分）。
