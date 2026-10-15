# Robust EA テンプレート戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MQLのRobust EAテンプレートを実装した戦略です。
エントリーシグナルの生成にCommodity Channel Index (CCI)とRelative Strength Index (RSI)を使用し、固定のテイクプロフィットとストップロスを適用します。

## ロジック
- CCIが-200..-150または-100..-50の範囲にあり、RSIが0から25の間のとき買い。
- CCIが50から150の間にあり、RSIが80から100の間のとき売り。
- ストップロスとテイクプロフィットはpipsで定義され、価格ポイントに変換される。

## パラメーター
- `Candle Type` – ローソク足データシリーズ。
- `CCI Period` – CCIインジケーターの期間。
- `RSI Period` – RSIインジケーターの期間。
- `Take Profit (pips)` – 利益目標までの距離。
- `Stop Loss (pips)` – ストップロスまでの距離。
- `Volume` – 注文数量。
