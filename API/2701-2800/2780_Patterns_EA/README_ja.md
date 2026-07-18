# パターン EA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
パターン EA 戦略は、最新の 3 本の完成したローソク足をスキャンし、単一、二重、三重バーの幅広い形成を検索する価格行動システムです。このロジックは、MQL5 の元のエキスパートアドバイザー「Patterns_EA」の StockSharp ポートで、30 種類のローソク足セットアップの設定可能なカタログを保持しています。各パターンは独立して有効化または無効化でき、ロングまたはショートの実行に割り当てることができ、戦略がソーススクリプトの裁量ルールを模倣できます。

## パターングループ
検出器は、パターングループに応じて現在のローソク足と最大 2 本前のローソク足を評価します：

- **グループ 1 – 一本バーパターン：** Neutral Bar、Force Bar Up、Force Bar Down、Hammer、Shooting Star。
- **グループ 2 – 二本バーパターン：** Inside、Outside、Double Bar High Lower Close、Double Bar Low Higher Close、Mirror Bar、Bearish Harami、Bearish Harami Cross、Bullish Harami、Bullish Harami Cross、Dark Cloud Cover、Doji Star、Engulfing Bearish Line、Engulfing Bullish Line、Two Neutral Bars。
- **グループ 3 – 三本バーパターン：** Double Inside、Pin Up、Pin Down、Pivot Point Reversal Up、Pivot Point Reversal Down、Close Price Reversal Up、Close Price Reversal Down、Evening Star、Morning Star、Evening Doji Star、Morning Doji Star。

許容パラメーター（Equality Pips）は、2 つの価格が等価チェックを満たすためにどれだけ一致する必要があるかを制御し、元の EA の「最大 pip 距離」設定を再現します。

## パラメーター
- **Candle Type** – パターン検出に使用する時間軸。
- **Opened Mode** – MQL バージョンから複製されたポジション管理ロジック（Any、Swing、Buy One、Buy Many、Sell One、Sell Many）。
- **Equality Pips** – 価格等価を定義する pip 距離；銘柄の価格ステップで調整。
- **Enable One-Bar Patterns / Enable Two-Bar Patterns / Enable Three-Bar Patterns** – 各パターングループのトグル。
- **Enable {Pattern}** – 30 の全形成の個別スイッチ。
- **{Pattern} Order** – 対応するパターンに割り当てられた取引方向（買いまたは売り）。

すべてのパラメーターは `StrategyParam` オブジェクトを通じて公開されており、StockSharp アプリケーション内で使用する際に最適化または UI バインディングを可能にします。

## トレードロジック
1. 戦略は設定されたローソク足シリーズを購読し、完成したローソク足を待ちます。
2. 新しいバーが閉じると、最新の 3 本のローソク足がキャッシュされ、検出器が有効なパターングループを評価します。
3. 各パターンは、許容比較とシャドウ/ボディの関係を含む MQL5 ソースからの条件ルールを複製します。
4. パターンが確認されると、`TriggerPattern` はグループと個別パターンが有効かどうかを確認し、選択した方向を検証し、設定されたポジションモードを適用します。
5. 戦略は割り当てられた方向に成行注文を送信します。スイングモードでは、最初に逆方向のオープンポジションを閉じます。

## ポジションモード
- **Any：** 追加の制約なしに両方向のシグナルを許可。
- **Swing：** 単一のネットポジションを維持；逆方向シグナルは新しいポジションに入る前に既存のポジションを解消。
- **Buy One / Sell One：** それぞれ単一のロングまたはショートポジションに取引を制限。
- **Buy Many / Sell Many：** 逆方向のシグナルを無視しながら、指定した方向への複数エントリーを許可。

## 注記
- アルゴリズムは `Security.PriceStep` を使用して等価許容度を絶対価格距離に変換します。銘柄が価格ステップを定義しない場合、デフォルトの 0.0001 が適用されます。
- 追加のインジケーターは不要；すべてのロジックはローソク足のジオメトリのみに依存しており、元のエキスパートアドバイザーの意図と一致しています。
