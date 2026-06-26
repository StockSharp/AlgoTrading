# Lego EA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Lego EA 戦略** は MetaTrader エキスパートアドバイザー「Lego EA」の直接ポートです。Commodity Channel Index、デュアル移動平均、ストキャスティクスオシレーター、Accelerator Oscillator、DeMarker、Awesome Oscillator という技術フィルターの設定可能な組み合わせを使用してエントリーとエグジットを検証します。各フィルターはエントリーとエグジットに対して独立してオン/オフを切り替えられるため、元の「Lego」をブロックごとに再構築したり、カスタムセットアップを試したりできます。

## パラメーター
- `Volume` – 前回の取引が利益だった場合に使用されるベーストレーディングボリューム。
- `LotMultiplier` – 損失取引後に最後に実行されたボリュームに適用される乗数（マーチンゲールスタイルの回復）。
- `StopLossPips` – ピップス単位の保護ストップ（シンボルのティックサイズを使用して内部変換されます）。
- `TakeProfitPips` – ピップス単位の利益目標。
- `UseCciForEntry` / `UseCciForExit` – ポジションを開くまたは閉じる際に CCI フィルターを有効にします。
- `UseMaForEntry` / `UseMaForExit` – 確認のために高速/低速移動平均クロスオーバーを使用します。
- `UseStochasticForEntry` / `UseStochasticForExit` – 設定された閾値内のストキャスティクス %K/%D の位置合わせを要求します。
- `UseAcceleratorForEntry` / `UseAcceleratorForExit` – Accelerator Oscillator の加速パターンを要求します。
- `UseDemarkerForEntry` / `UseDemarkerForExit` – DeMarker レベルチェックを適用します。
- `UseAwesomeForEntry` / `UseAwesomeForExit` – Awesome Oscillator のモメンタム確認を含めます。
- `CciPeriod` – Commodity Channel Index の期間。
- `MaFastPeriod` / `MaSlowPeriod` – 高速および低速移動平均のルックバック長。
- `MaShift` – MT5 の水平シフトパラメーターを再現して移動平均値を時間的に後ろにシフトする完了バーの数。
- `MaMethod` – 平滑化メソッド（シンプル、指数、平滑、または加重）。
- `MaPrice` – 両方の移動平均に供給されるローソク足価格ソース。
- `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlow` – ストキャスティクスオシレーターの設定。
- `StochasticLevelUp` / `StochasticLevelDown` – シグナルに使用される買われすぎ/売られすぎ閾値。
- `DemarkerPeriod`, `DemarkerLevelUp`, `DemarkerLevelDown` – DeMarker オシレーター設定。
- `CandleType` – すべてのインジケーターが使用するローソク足シリーズの時間軸。

## 取引ワークフロー
1. 完了したローソク足ごとに、ストラテジーは選択されたフィルターからインジケーター値を収集します。
2. 各フィルターは前の完全に形成されたバーに基づいて買い/売りの準備状況を計算します（元の EA の `iGetArray(..., 1)` オフセットと一致）。
3. ロングエントリーは**すべての有効なエントリーフィルター**が強気シグナルに同意した場合のみ許可されます。同様に、ショートエントリーは満場一致の弱気確認を必要とします。
4. アカウントがフラットで有効なエントリーシグナルが現れた場合、ベース `Volume` または最後の損失取引ボリューム × `LotMultiplier` を使用した成行注文が送信されます。
5. すでにポジションがある場合、有効なエグジットフィルターが同じように評価されます。ポジションはすべてのエグジットフィルターが反対シグナルに同意した場合のみクローズされます。
6. ストップロスとテイクプロフィットの保護はシンボルのティックサイズに基づいてピップ入力を絶対価格距離に変換して `StartProtection` を使用して自動的にインストールされます。

## 資金管理
- 勝ち取引後、次の注文はベース `Volume` に戻ります。
- 負け取引後、ボリュームは `LotMultiplier` で乗算され、元の EA のロットエスカレーションロジックをエミュレートします。
- 取引所が課すボリューム制限（ステップ、最小、最大）は各注文の前に適用されます。

## MetaTrader バージョンとの注意事項と違い
- インジケーターの価格ソースは StockSharp の同等物にマップされます。CCI は内部的に典型価格を使用し、移動平均は選択された `MaPrice` ソースを使用します。
- すべてのインジケーター計算は完全に閉じたローソク足に依存します。これにより部分的に形成されたデータを回避し、EA の「新しいバー」処理を模倣します。
- フリーズレベルチェックと手動 SL/TP 価格配置は StockSharp の `StartProtection` サービスによって処理されます。
- 部分的なポジションエグジットは、ポジション全体がフラットの場合のみ損失追跡状態を更新し、EA の `DEAL_ENTRY_OUT` ロジックと一致します。

## 使用上のヒント
- オリジナルの設定（MA フィルター有効、他のフィルター無効）から始めてベース動作を再現し、その後追加フィルターを有効にしてシグナル品質を上げます。
- 高い `LotMultiplier` 値を使用する際はアカウントのエクスポージャーを監視してください。損失が続くとリスクが急速に増大します。
- バックテスターと戦略を組み合わせて、選択したフィルターの組み合わせが取引する予定の楽器と一致するかどうかを確認します。

この戦略は現在 Python バージョンを持ちません。
