# JBrainTrend ReOpen 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は MQL5 のサンプル「JBrainTrend1Stop_ReOpen」に触発された C# の実装です。  
ストキャスティクスオシレーターを使用して買われすぎ・売られすぎの状態を判断し、価格が指定されたステップだけ進んだ時にポジションを再開することでピラミッディングをサポートします。

## ロジック
- 選択された時間軸のローソク足を購読する。
- ストキャスティクスオシレーター（%K と %D）を計算する。
- %K が 20 を下回ったらロングに入り、80 を上回ったらショートに入る。
- 反対の極値に達したらポジションを閉じる。
- エントリー後、価格が取引の方向に `PriceStep` 動いた場合、`MaxPositions` まで追加ポジションを加える。
- 防護的なストップロスとテイクプロフィットを絶対価格単位で適用する。

## パラメーター
- `StochPeriod` – ストキャスティクスオシレーターの主要期間。
- `KPeriod` / `DPeriod` – %K と %D ラインの平滑化期間。
- `CandleType` – 分析に使用する時間軸。
- `StopLoss` – 価格単位でのストップロス距離。
- `TakeProfit` – 価格単位でのテイクプロフィット距離。
- `PriceStep` – ポジションを再開するために必要な価格移動。
- `MaxPositions` – 一方向への最大エントリー数。
- `BuyEnabled` / `SellEnabled` – ロング/ショート取引を有効または無効にする。

## 注記
元の MQL5 スクリプトは *JBrainTrend1Stop* という名前のカスタムインジケーターを使用していました。  
この C# ポートは、StockSharp の組み込みインジケーターを使って取引コンセプトを近似し、統合を容易にしています。
