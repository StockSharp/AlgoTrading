# EMA クロス約定アラート戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、確定済み 1 分足で期間 120 の短期 EMA と期間 450 の長期 EMA の上向き・下向きクロスを取引します。ポジションのスナップショットで各シグナルを絞り込み、固定数量の成行注文で建玉を管理し、ストラテジー自身のすべての約定をログに記録します。チャートにはローソク足、2 本の EMA、買いと売りの約定が表示されます。

![schema](schema.svg)

## 戦略の概要

- 確定済み 1 分足が短期 EMA 120 と長期 EMA 450 に入力されます。両インジケーターで形成済み値だけに限定するフィルターは無効なため、計算開始時から値が出力されます。
- Crossing ブロックは、短期 EMA が長期 EMA を上抜けたときに `true` を出力します。NOT ブロックは、下抜けイベントの `false` を売り側のトリガー信号に変換します。
- 各足の評価時には、足で起動されるスナップショットが EMA シグナルの処理前に現在のポジションを出力します。比較条件により、買いは `Position <= 0`、売りは `Position >= 0` の場合だけ許可されます。
- 2 つの成行注文ブロックは `NoCondition` と固定 Volume 1 を使用します。反対シグナルはポジションを縮小またはゼロにでき、ポジションの絶対値が Volume 未満ならゼロをまたぐこともありますが、完全な反転は保証されません。
- Strategy trades ブロックは、ストラテジー自身のすべての約定を正確な約定メッセージのテンプレートで Log 通知へ送ります。チャートは確定済み足、2 本の EMA、買いと売りの約定ストリームを受け取ります。

## エントリーとエグジットの条件

- **ロングエントリー**: 短期 EMA が長期 EMA を上抜け、その足の評価時点のポジションスナップショットがゼロ以下の場合、ダイアグラムは Volume 1 の成行買い注文を送信します。
- **ショートエントリー**: 短期 EMA が長期 EMA を下抜け、その足の評価時点のポジションスナップショットがゼロ以上の場合、ダイアグラムは Volume 1 の成行売り注文を送信します。
- **エグジット**: 専用の決済ブロックや保護ブロックはありません。後で条件を満たした反対方向の固定数量注文により、現在のポジションを縮小したり、同量ならゼロにしたり、ポジションの絶対値が Volume より小さければゼロをまたいだりできますが、完全な反転は保証されません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 00:01:00 | 1 分足の時間枠です。確定済みの足だけが EMA と判定チェーンを動かします。 |
| Fast EMA Period | 120 | 短期 ExponentialMovingAverage の期間です。形成済み値だけに限定するフィルターは無効です。 |
| Slow EMA Period | 450 | 長期 ExponentialMovingAverage の期間です。形成済み値だけに限定するフィルターは無効です。 |
| Volume | 1 | 2 つの NoCondition 成行注文ブロックに渡す固定数量です。 |

## ダイアグラムの詳細

- [ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは確定済み 1 分足だけを出力し、EMA 計算へ渡す前にポジションのスナップショットを起動します。
- 2 つの[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックが、期間 120 と 450 の ExponentialMovingAverage を計算します。形成済み値のオプションはいずれも `false` で、両方の出力はチャートにも送られます。
- [クロス](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)出力は上抜けで `true`、下抜けで `false` になります。NOT の[論理条件](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)が下抜けイベントを正の売りトリガーに変換し、個別の AND ブロックが方向とポジションを組み合わせます。
- 現在の[ポジション](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/current.html)は継続的に保存され、EMA 経路より前に足ごとに 1 回出力されます。[比較](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)ブロックは、クロスと同じローソク足の処理フロー内で `Position <= 0` と `Position >= 0` を評価します。
- 買いと売りの[ポジション変更](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ブロックは、`NoCondition` と共通の固定 Volume 値で成行注文を出します。ストップロス、テイクプロフィット、個別の決済ブロックはありません。
- [ストラテジー取引](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html)はストラテジー自身の各 `MyTrade` を出力します。[文字列フォーマッター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html)は `EMA cross fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}` を正確に使用します。
- [通知](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html)ブロックは、Type `Log`、Caption `EMA cross trade` でフォーマット済みの各約定を記録します。チャートはローソク足、短期 EMA、長期 EMA、個別の買いと売りの約定ストリームを描画します。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
