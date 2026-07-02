# ネクストバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**ネクストバー戦略**は、MetaTrader 4 エキスパート アドバイザー `nextbar.mq4` の直訳です。オリジナルの EA は、最後に完了したローソク足と数バー古いローソク足との間の距離を評価します。価格が一方向に十分に移動すると、構成された方向フラグに応じて、勢いに従うか、勢いに逆らって取引されます。その後、ポジションは対称的なテイクプロフィット/ストップロスレベルで保護され、一定数のバーが経過すると強制的にクローズされます。

この StockSharp バージョンは、高レベルの戦略 API を使用している間、同じ動作を維持します。完了したローソク足のみを処理し、すべての計算が MT4 スクリプトのバーオンクローズ ロジックと一致することを保証します。

## 元の MQL ロジック
* **運動量距離** – `Close[1]` と `Close[bars2check+1]` を比較します。差が少なくとも `minbar * Point` である場合、それを有効な信号として扱います。
* **Direction flag** – the MQL input `direction` equals `1` for trend-following (buy after a rally, sell after a drop) and `2` for contrarian trading (buy after a drop, sell after a rally).
* **エントリーの制約** – 一度にオープンできる注文は 1 つだけです。新しい取引はシグナルに続いてバーの開始時に送信されます。
* **エグジットルール** – 最後の終値がエントリーより上の利益距離、またはそれより下の損失距離に達した場合はロングを閉じます。ショートの場合はその逆が当てはまります。どちらのレベルにも達していない場合は、`bars2hold` のローソク足が完了した後に取引を終了します。

## StockSharp 実装のハイライト
* `SubscribeCandles()` と `Bind` を使用して、設定された時間枠で完了したローソク足を受け取ります。
* MQL `bars2check + 1` オフセットに一致するローソク足を参照するために、終値の短いローリング履歴を保存します。
* すべてのポイントベースのパラメータを `Security.PriceStep` で変換し、MetaTrader `Point` 定数を模倣します。
* 戦略 `Volume` を使用して成行注文を出し、`Direction` パラメーターを介してモメンタム追従または逆張りのエントリーをサポートします。
* 元のワークフローとの整合性を維持するために、利益、損失、および保持期間の終了を完成したキャンドルごとに 1 回だけ実装します。

## パラメーター
| パラメータ | 説明 | デフォルト | 注意事項 |
|-----------|-------------|---------|-------|
| `CandleType` | 信号評価に使用される時間枠。 | 1時間枠 | このローソク タイプを提供できる証券にストラテジーをアタッチします。 |
| `BarsToCheck` | 基準終値と最新終値の間で完了したローソク足の数。 | 8 | EA の `bars2check` と一致します。 |
| `BarsToHold` | オープンポジションを維持するための完了したローソクの最大数。 | 10 | `bars2hold` と一致します。カウンタがこの数値に達したバーでポジションはクローズされます。 |
| `MinMovePoints` | 比較された 2 つの終値間の最小距離 (MetaTrader ポイント単位)。 | 77 | `minbar`に対応します。 `Security.PriceStep` を使用して変換されました。 |
| `TakeProfitPoints` | 目標距離を MetaTrader ポイント単位で獲得します。 | 115 | `profit` 入力と同等。必要に応じて無効にするには、ゼロに設定します。 |
| `StopLossPoints` | MetaTrader ポイント単位のストップロス距離。 | 115 | `loss` 入力と同等。必要に応じて無効にするには、ゼロに設定します。 |
| `Direction` | 取引モード: `Follow` (トレンド) または `Reverse` (逆張り)。 | `Follow` | `direction` 入力をミラーリングします (`1` = フォロー、`2` = リバース)。 |
| `Volume` | 成行注文に使用される取引量。 | 戦略ボリューム | 標準の `Strategy.Volume` プロパティを通じて設定します。 |

## 取引ワークフロー
1. ローソクが完成するのを待って、その終値をキャッシュします。
2. `BarsToCheck` 個前のローソク足から終値を取得し、差を計算します。
3. 絶対移動が `MinMovePoints * PriceStep` 未満の場合は何もしません。
4. それ以外の場合:
   * **フォロー** モードでは、価格が上昇した場合は買い、価格が下落した場合は売ります。
   * **リバース** モードでは、価格が下落した場合は買い、価格が上昇した場合は売ります。
5. ポジションがオープンしている間、後続の終了したキャンドルごとに:
   * 終値が保存されたエントリー価格より `TakeProfitPoints` 高いか、`StopLossPoints` 低い場合、終値ロングとなります。
   * 終値がエントリーより`TakeProfitPoints`下か`StopLossPoints`上になったらショートを閉じます。
   * エントリーから `BarsToHold` キャンドルが経過したら強制終了します。

## 使用上の注意
* ポイントから絶対価格への変換には、`Security.PriceStep` が必要です。戦略を実行する前に、正しい商品メタデータ (価格ステップ、ステップ価格、出来高ルール) を提供してください。
* この戦略は複数の同時ポジションを管理しません。 `Volume` が 1 回の MT4 注文で予想されるサイズに対応していることを確認してください。
* Because decisions are evaluated on completed candles only, the strategy should be run with historical and real-time data that deliver finished bars.
