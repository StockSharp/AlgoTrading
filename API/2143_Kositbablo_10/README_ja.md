# Kositbablo 10
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIとEMAシグナルを使用したEURESDのマルチタイムフレーム戦略。
日足と時間足のインジケーターをチェックし、両方のトレンドフィルターが一致したときに成行注文を出します。

## パラメーター
- **Take Profit** – ポイント単位のテイクプロフィット。
- **Stop Loss** – ポイント単位のストップロス。
- **Turbo Mode** – ポジションが存在していても新規取引を許可する。

## ルール
- 日足RSI(11) < 60、時間足RSI(5) < 48、かつEMA20 > EMA2のときロングエントリー。
- 日足RSI(22) > 38、時間足RSI(20) > 60、かつEMA23 > EMA12のときショートエントリー。
- 時間足ローソク足の確定後にのみ取引する。
