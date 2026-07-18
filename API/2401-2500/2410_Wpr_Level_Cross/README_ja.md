# WPR レベルクロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は Williams %R オシレーターが事前定義された買われすぎ・売られすぎレベルをクロスする際に取引します。

インジケーターが **Low Level** を下回るとき、売られすぎ状態からの潜在的な反転を示します。**High Level** を上回るとき、買われすぎ状態からの反転の可能性を示します。選択した **Trend Mode** に応じて、戦略はインジケーターの方向に取引するか、逆張り取引のためにシグナルを反転させることができます。

## パラメーター

- `WprPeriod` – Williams %R のルックバック期間。
- `HighLevel` – 買われすぎのしきい値。
- `LowLevel` – 売られすぎのしきい値。
- `Trend` – `Direct` はインジケーターシグナルに従って取引し、`Against` はそれらを反転させます。
- `EnableBuyEntry` / `EnableSellEntry` – ロング/ショートポジションへのエントリーを許可。
- `EnableBuyExit` / `EnableSellExit` – ショート/ロングポジションのクローズを許可。
- `StopLoss` – 価格単位でのストップロス値。
- `TakeProfit` – 価格単位でのテイクプロフィット値。
- `CandleType` – 計算に使用するローソク足の時間軸。

## 仕組み

1. 戦略はローソク足を購読し、Williams %R インジケーターを計算します。
2. 各完成したローソク足でインジケーターが指定したレベルをクロスしたかどうかを確認します。
3. `Trend` と有効なアクションに応じて、成行注文を使ってポジションを開くか閉じます。
4. オプションのストップロスとテイクプロフィット保護は `StartProtection` を通じて有効になります。

## 注記

- コード内のコメントは英語で提供されています。
- C# バージョンのみが実装されており、Python バージョンは意図的に省略されています。
