# Omzdwwi 保留中のマネージャー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

**Omzdwwi 保留中のマネージャー戦略** は、MetaTrader 4 の専門家 `omzdwwi7739cyjayvs_1_65.mq4` の高レベルの StockSharp を直接翻訳したものです。オリジナルのアドバイザーは、現在の市場価格付近で未決注文のリングを維持し、スケジュールされたタイマーで市場エントリーを実行し、アクティブなポジションと未処理の未決注文の両方のトレーリングストップを管理することに重点を置いています。この C# バージョンは、StockSharp の `Strategy` API、`SubscribeLevel1` フィード、注文管理ヘルパー (`BuyStop`、`SellLimit`、`ReRegisterOrder` など) を活用しながら、同じロジックを再現します。

戦略は継続的に次のとおりです。

- 最大 4 つの未決注文 (買いストップ、売りストップ、買い指値、売り指値) を、買値/売値から設定可能な距離に保持します。
- オプションで、特定の時間と分に市場の買い/売り注文を発行します。
- 市場ポジションのエグジットの複数のレイヤーを適用します: 固定テイクプロフィット、固定ストップロス、追加の「pips 利益」ターゲット、エキスパートの `TrailingPositions()` ルーチンを模倣したトレーリング ストップ ロジック。
- 市場が設定されたトレーリング距離だけ前進すると、エキスパートの `TrailingOtlozh()` ルールに従って未決注文を価格から近づけたり遠ざけたりします。
- アカウントレベルの利益と損失のしきい値を監視し、設定されたグローバルなテイクプロフィットまたはストップロスのパーセンテージに達したときに情報/警告ログを出力します。

## シグナルフローとデータサブスクリプション

- `SubscribeLevel1()` は入札/売値の最新情報を配信します。相場が更新されるたびに、時間チェック、注文発注、トレーリング調整、および終了チェックがトリガーされます。ローソク足のデータやインジケーターは必要ありません。
- `GetWorkingSecurities()` はレベル 1 サブスクリプションを宣言するため、戦略はライブ環境とバックテスト環境の両方で実行できます。

## エントリーロジック

1. **スケジュールされた成行注文。** `UseTimeSignals` が有効でサーバー クロックが `SignalHour:SignalMinute` に達すると、ストラテジーは `Time*Signal` パラメーターから派生したブール ラッチを生成します。次のレベル 1 アップデートは、`WaitClose`/`MaxMarketOrders` で許可されている場合に、`BuyMarket()` または `SellMarket()` を呼び出します。ラッチは取引直後にリセットされます。
2. **永続的な保留中の注文。** 有効な注文タイプ (`EnableBuyStop`、`EnableSellStop`、`EnableBuyLimit`、`EnableSellLimit`) ごとに、ストラテジーはアクティブな注文があることを確認します。不在注文は最良の買値/売値から `Distance * PriceStep` ポイントで発注され、エキスパートの `UstanOtlozh()` の動作を再現します。注文がすでに存在する場合、`ReRegisterOrder` は価格を現在の相場に合わせて維持します。

## 市場ポジションの出口ロジック

- **固定ストップロス/テイクプロフィット**は、`MarketStopLossPoints` と `MarketTakeProfitPoints` からのものです。最良の買い/売りがこれらのしきい値を超えると、ポジションは成行注文によってフラット化されます。
- **追加の pips ターゲット** は、エキスパートの `PipsProfit` の動作を再現します。ゼロ以外の場合、TP が無効であっても、設定された利益を獲得した後にポジションをクローズします。
- **トレーリングストップ**は `TrailingPositions()` をコピーします。ポジションが十分に収益性を高めると（`RequireProfitBeforeTrailing=false` の場合は直ちに）、内部トレーリング価格は、`MarketTrailingStepPoints` によって強制される最小トレイル ステップにより、ロングの場合は `Bid - MarketTrailingOffsetPoints * PriceStep`、ショートの場合は `Ask + MarketTrailingOffsetPoints * PriceStep` に更新されます。

## 未決注文のトレーリングロジック

- ストップ注文は `StopTrailingOffsetPoints` と `StopTrailingStepPoints` を使用します。価格が MQL のしきい値 (買いストップの場合は `Ask < OrderPrice - (offset + step)`、売りの場合は対称) を超えると、注文は `Ask + offset` または `Bid - offset` に再登録されます。
- 指値注文では、同じ方法で `LimitTrailingOffsetPoints` と `LimitTrailingStepPoints` を使用し、`TrailingOtlozh()` 調整を再作成します。

## リスクとアカウントのモニタリング

- `MaxMarketOrders` は、`WaitClose=false` の場合、方向ごとに蓄積できるロット数 (`OrderVolume` の倍数で表されます) を制限します。
- `UseGlobalLevels`、`GlobalTakeProfitPercent`、および `GlobalStopLossPercent` はポートフォリオの資本を監視します。しきい値を超えると、戦略は元の警告ポップアップを反映して情報または警告ログを書き込みます。

## パラメーター

| グループ | パラメータ | 説明 |
|-------|-----------|-------------|
| 一般 | `OrderVolume` | すべての注文で再利用される取引量 (ロット)。 |
| 実行 | `WaitClose` | ネットポジションがフラットになるまで新規エントリーをブロックします。 |
| 実行 | `MaxMarketOrders` | ピラミッド化が許可されている場合の、方向ごとの最大同時ロット。 |
| 未決注文 | `EnableBuyStop` / `EnableSellStop` / `EnableBuyLimit` / `EnableSellLimit` | 各未決注文タイプを有効または無効にします。 |
| 未決注文 | `StopStepPoints`, `LimitStepPoints` | 現在の買値/売値に対する逆指値/指値注文の発注に使用されるポイント単位の距離。 |
| 未決注文 | `StopTakeProfitPoints`, `StopStopLossPoints`, `LimitTakeProfitPoints`, `LimitStopLossPoints` | 未決注文がトリガーされると保護距離が適用されます。 |
| 未決注文 | `StopTrailingOffsetPoints`, `StopTrailingStepPoints`, `LimitTrailingOffsetPoints`, `LimitTrailingStepPoints` | 未処理の指値注文のトレーリングパラメータ。 |
| 市場リスク | `MarketTakeProfitPoints`, `MarketStopLossPoints` | 市場ポジションのポイントでのテイクプロフィットとストップロス。 |
| 市場リスク | `MarketTrailingOffsetPoints`, `MarketTrailingStepPoints`, `RequireProfitBeforeTrailing` | 市場ポジションのトレーリングストップ設定。 |
| 市場リスク | `ExitProfitPoints` | 追加の固定利益目標。 |
| 時間管理 | `UseTimeSignals`, `SignalHour`, `SignalMinute` | スケジュール実行の設定。 |
| 時間管理 | `TimeBuySignal`, `TimeSellSignal`, `TimeBuyStopSignal`, `TimeSellStopSignal`, `TimeBuyLimitSignal`, `TimeSellLimitSignal` | タイマーが作動したときにどの注文をトリガーするか。 |
| アカウントの監視 | `UseGlobalLevels`, `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | ポートフォリオレベルのアラートしきい値。 |
| その他 | `SlippagePoints` | 予約済みのレガシーパラメータは完全を期すために維持されています。 |

## 変換メモ

- MQL のエキスパートは、未決注文に直接テイクプロフィット/ストップロスを設定します。 StockSharp は保留中のエントリを最初に配置し、次に戦略ロジックを通じて終了を管理して、実装を高レベルの API 制約内に保ちます。
- StockSharp のロギングではすでに構造化された通知が提供されているため、サウンド アラートは省略されました。
- MetaTrader の `MODE_STOPLEVEL` 制約は StockSharp には存在しません。したがって、パラメータは取引所が課す最小距離を尊重するかどうかに依存します。
- エラー処理では、`Alert()` ポップアップの代わりに `AddInfoLog`/`AddWarningLog` を使用します。

## 使用法

1. 有効な価格ステップを使用して戦略を `Security` と `Portfolio` に接続します。
2. 距離をポイント単位で設定します (証券の `ShrinkPrice` を使用して価格単位に自動的に変換されます)。
3. 戦略を開始します。レベル 1 の見積もりを購読し、すぐに注文の管理を開始します。

> **ヒント:** バックテストを行うときは、元の MQL エキスパートと同じように、トレーリング ロジックとタイミング ロジックがすべての見積もりの更新を受信できるように、テスターがレベル 1 データをフィードするようにしてください。
