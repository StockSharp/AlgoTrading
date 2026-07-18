# Exp Adaptive Renko MMRec Duplex戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTrader 5のエキスパートアドバイザー **Exp_AdaptiveRenko_MMRec_Duplex.mq5** をStockSharpの高レベルAPIにポートしたものです。2つの独立したAdaptive Renkoストリーム — ロングの機会用に設定されたものとショート用のもの — が、カスタムレンガチャンネルがサポートとレジスタンス間でどのようにフリップするかを観察します。ロングチャンネルが新しいサポートを報告し、ショートチャンネルがレジスタンスを失う（またはその逆）と、戦略は対応する市場ポジションを開きます。C#バージョンは、設定可能な一連の損失後にトレードサイズを削減し、連続が終わると復元するオリジナルの「MM Recounter」マネーマネジメントブロックを保持します。

## コアワークフロー

1. **データサブスクリプション** – 各サイドは自身のローソク足タイプ（時間軸）をサブスクライブし、`SubscribeCandles().BindEx(...)` を通じてボラティリティインジケーター（ATRまたは標準偏差）をバインドします。インジケーターが適応的なレンガの高さを駆動します。
2. **Adaptive Renko処理** – ヘルパー `AdaptiveRenkoProcessor` がMQLインジケーターのロジックを再構築し、最新のトレンド、サポートおよびレジスタンスレベルを含むスナップショットを返します。シグナルは完成したローソク足のみで評価されます。
3. **エントリーロジック** – ロングRenkoスナップショットが上昇トレンドを示す（シグナルバーにサポートが出現）と、戦略はロングポジションを開きます。ショートエントリーはショートストリームからの下落トレンドを要求します。
4. **エグジットロジック** – 反対のRenkoイベントがアクティブなポジションを閉じます。追加のチェックがストップロスとテイクプロフィットの距離を価格ステップで適用します。
5. **MMRecマネーマネジメント** – 各方向は最近実現したPnL値のキューを維持します。設定されたウィンドウ内の損失数が損失トリガーに達すると、次の注文は削減されたマネーマネジメント値（`LongSmallMoneyManagement` / `ShortSmallMoneyManagement`）を使用します。そうでない場合は通常値（`LongMoneyManagement` / `ShortMoneyManagement`）が使用されます。`MarginModeOption` enumがMQLのサイジングモード（ロット、残高比率、損失ベース比率等）を再現します。
6. **取引登録** – 各エグジットが `RegisterTradeResult` を呼び出してMMRecキューに供給します。キューのトリミングはターミナル履歴をスキャンせずにオリジナルの `BuyTradeMMRecounterS` と `SellTradeMMRecounterS` 関数を反映します。

## パラメーターグループ

| グループ | 主要パラメーター | 説明 |
| --- | --- | --- |
| ロングサイド | `LongCandleType`, `LongVolatilityMode`, `LongVolatilityPeriod`, `LongSensitivity`, `LongPriceMode`, `LongMinimumBrickPoints`, `LongSignalBarOffset` | ロングエントリーを生成するAdaptive Renkoストリームを制御します。 |
| ショートサイド | `ShortCandleType`, `ShortVolatilityMode`, `ShortVolatilityPeriod`, `ShortSensitivity`, `ShortPriceMode`, `ShortMinimumBrickPoints`, `ShortSignalBarOffset` | ショートモジュールの設定を反映します。 |
| MMRec | `LongTotalTrigger`, `LongLossTrigger`, `LongSmallMoneyManagement`, `LongMoneyManagement`, `LongMarginMode`, `ShortTotalTrigger`, `ShortLossTrigger`, `ShortSmallMoneyManagement`, `ShortMoneyManagement`, `ShortMarginMode` | マネーマネジメント回復ブロックを複製します。*TotalTrigger*パラメーターはローリングウィンドウサイズを定義し、*LossTrigger*は削減ボリュームをアクティブにする損失数を定義します。 |
| リスク | `LongStopLossPoints`, `LongTakeProfitPoints`, `ShortStopLossPoints`, `ShortTakeProfitPoints`, `LongDeviationSteps`, `ShortDeviationSteps` | 保護レベルと情報的なスリッページを価格ステップで表します。 |

## 動作ノート

- 戦略はネッティング口座モデルで動作します：ロングトレードを開く前に未決のショートを閉じ、その逆も同様です。
- ポジションサイズは `CalculateVolume` を通じて計算されます。ヘルパーは設定されたストップロス距離に依存する損失ベースのサイジングを含む全ての元のマージンモードをサポートします。
- すべてのインジケーター処理はソースEAを尊重して完成したローソク足のみで行われます。
- ログにはトレーサビリティのためにマネーマネジメント乗数と期待されるスリッページ（ステップ単位）が含まれます。

## ファイル

- `CS/ExpAdaptiveRenkoMmrecDuplexStrategy.cs` – Adaptive Renkoプロセッサとで MMRecモジュールを含む戦略実装。
- `README.md` – 英語ドキュメント（このファイル）。
- `README_ru.md` – ロシア語ドキュメント。
- `README_zh.md` – 中国語ドキュメント。
