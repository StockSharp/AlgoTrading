# Forex Fraus ポートフォリオ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、長期の **Williams %R** インジケーターに基づいて単一の銘柄を取引します。インジケーターが極端なゾーンを離れると、戦略はブレイクアウトの方向にポジションを開設します。

## 機能の仕組み

1. `WprPeriod` 本のローソク足でWilliams %Rを計算します。
2. インジケーターが `BuyThreshold` を下回ると、ロングの機会が準備されます。閾値を上回ると、成行買い注文が出されます。
3. インジケーターが `SellThreshold` を上回ると、ショートの機会が準備されます。閾値を下回ると、成行売り注文が出されます。
4. ポジションは `StartHour` と `StopHour` の間の時間窓内でのみ許可されます。
5. オプションのストップロス、テイクプロフィット、トレーリングストップをパラメーターで有効にできます。

## パラメーター

- `WprPeriod` – Williams %Rの期間。
- `BuyThreshold` – ロングシグナルを有効にする値。
- `SellThreshold` – ショートシグナルを有効にする値。
- `StartHour` / `StopHour` – 取引セッションの制限。
- `SlPoints` – ポイント単位のストップロス。0で無効。
- `TpPoints` – ポイント単位のテイクプロフィット。0で無効。
- `UseTrailing` – トレーリングストップロジックを有効にする。
- `TrailingStop` – ポイント単位のトレーリング距離。
- `TrailingStep` – トレーリング更新のステップ。
- `CandleType` – 購読するローソク足の種類。

## 備考

オリジナルのMQL4バージョンは複数の通貨ペアを取引し、それぞれの注文を管理していました。このC#移植版は単一の銘柄に焦点を当て、StockSharpの高レベルAPIを使ってコアアイデアを示しています。
