# ProfitLossTrailStrategy戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

ProfitLossTrailStrategy は、MetaTrader エキスパートアドバイザー **ProfitLossTrailEA v2.30** から変換されたリスク管理ヘルパーです。この戦略は自分でエントリーを生成しません。代わりに、設定された銘柄の現在オープンポジションを監視し、保護エグジットを自動適用します。

- 初期 stop-loss と take-profit 水準。
- 任意の起動距離と trailing step 制御を備えた trailing stop 管理。
- 設定可能な利益トリガーとオフセットを持つ break-even 保護。
- トレーダーが手動で管理したい場合に既存の保護水準を削除する機能。

動作は、元 EA の「バスケット」管理モードに非常に近いものです。同じ方向のすべての注文は単一ポジションとして扱われ、エクスポージャーが変わるたびに保護水準が再計算されます。

## パラメーターリファレンス

| パラメーター | 説明 |
|-----------|------|
| **Manage As Basket** | 有効 (デフォルト) の場合、同じ方向の各 fill が平均エントリー価格を再計算し、stop-loss/take-profit 水準を更新します。最初の fill 後に初期水準を維持するには、このフラグを無効にします。 |
| **Enable Take Profit** | 自動 take-profit 処理をオン/オフします。 |
| **Take Profit (pips)** | エントリー価格と take-profit 目標の間の pip 距離。 |
| **Enable Stop Loss** | 自動 stop-loss 処理をオン/オフします。 |
| **Stop Loss (pips)** | エントリー価格と初期保護ストップの間の pip 距離。 |
| **Enable Trailing Stop** | ポジションが利益状態になった後の動的 stop 管理を有効にします。 |
| **Trailing Activation (pips)** | trailing stop が動く前に必要な最小利益 (pips)。即時有効にするには `0` を使用します。 |
| **Trailing Stop (pips)** | pips で表される基本 trailing 距離。 |
| **Trailing Step (pips)** | trailing stop をさらに引き締める前に必要な追加利益。 |
| **Enable Break-Even** | トリガー距離後に stop を利益側へ移動する break-even ルーチンを有効にします。 |
| **Break-Even Trigger (pips)** | break-even 移動を有効にする利益距離。 |
| **Break-Even Offset (pips)** | break-even が有効になったとき、エントリー価格の上 (ロング) または下 (ショート) に追加されるオフセット。 |
| **Remove Take Profit** | `true` に設定すると、現在の take-profit 値がクリアされ、take-profit エグジットは発行されません。 |
| **Remove Stop Loss** | `true` に設定すると、現在の stop-loss 値がクリアされ、stop-loss または trailing エグジットは発行されません。 |
| **Candle Type** | 価格動向を監視するローソク足系列。trailing、break-even、エグジットチェックは確定ローソク足で評価されます。 |

## 使用上の注意

1. 戦略を銘柄に接続し、注文が外部または別の戦略から発注されることを確認します。ProfitLossTrailStrategy はオープンエクスポージャーの管理だけに集中します。
2. 商品の価格設定に合わせて pip ベースのパラメーターを設定します。pip サイズは `Security.PriceStep` から自動的に導出されます。
3. break-even と trailing stop の両方が有効な場合、break-even 調整が先に行われます。その後の trailing step は、新しい水準が現在の保護価格を少なくとも指定 trailing step 距離だけ改善する場合にのみ stop を引き締めます。
4. **Remove Stop Loss** を設定すると、元 EA の動作を反映し、stop-loss、trailing、break-even ロジックが同時に無効になります。
5. 戦略は保護水準に達したとき、成行注文 (`BuyMarket`/`SellMarket`) でポジションを閉じます。

## 変換メモ

- MetaTrader の "Order_By_Order" と "Same_Type_As_One" モードは、**Manage As Basket** フラグで表されます。StockSharp では ticket ごとの stop 水準管理はサポートされないため、バスケットモードがデフォルトで適用されます。
- 元 EA の magic number とコメントフィルターは不要です。戦略は設定された `Strategy.Security` のみに作用します。
- StockSharp はログとチャートバインディングで診断を提供するため、画面描画、音声アラート、タイマーベースの UI 更新は省略されています。
