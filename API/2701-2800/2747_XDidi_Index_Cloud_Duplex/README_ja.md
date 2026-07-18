# XDidi Index Cloud Duplex ストラテジー
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
XDidi Index Cloud Duplex ストラテジーは、元のMQL5エキスパート*Exp_XDidi_Index_Cloud_Duplex*のデュアルロング/ショートシグナリングロジックを再現します。2つの独立したXDidiインデックス設定が設定可能な時間軸で評価されます。各設定は、高速/中速と低速/中速の移動平均の比率を計算します。これらの比率間のクロスがマーケットエントリーを引き起こし、持続的な乖離がエグジットを引き起こします。

## トレードロジック
1. **インジケーター計算**
   - 各ブロックに対して選択した価格ソースで3つの移動平均（高速、中速、低速）が計算されます。
   - XDidi比率は`fast / medium`と`slow / medium`として導出されます。オプションの反転は元の`Revers`オプションに対応します。
2. **シグナル生成**
   - ロングブロック：前のバーが`fast > slow`で、シグナルバーが`fast <= slow`で閉じる場合、ロングエントリーが要求されます。前のバーが`fast < slow`の場合、ロングエグジットが要求されます。
   - ショートブロック：前のバーが`fast < slow`で、シグナルバーが`fast >= slow`で閉じる場合、ショートエントリーが要求されます。前のバーが`fast > slow`の場合、ショートエグジットが要求されます。
   - シグナルバーオフセットは元の`SignalBar`入力を再現します。
3. **注文管理**
   - エントリーはストラテジーボリュームで実行されます。逆のポジションは反転前に閉じられます。
   - オプションのストップロスとテイクプロフィットレベルは、価格ステップ距離を使用して`StartProtection`経由で適用されます。

## パラメーター
| 名前 | 説明 |
| --- | --- |
| `LongCandleType`, `ShortCandleType` | 各ブロックのローソク足の時間軸。 |
| `LongFastMethod` / `Medium` / `Slow` & `ShortFastMethod` / `Medium` / `Slow` | 高速、中速、低速カーブの移動平均スムージング手法。サポートされていないレガシースムーザーは指数平滑化にフォールバックします。 |
| `LongFastLength`, `LongMediumLength`, `LongSlowLength` | ロングブロックの移動平均のピリオド。 |
| `ShortFastLength`, `ShortMediumLength`, `ShortSlowLength` | ショートブロックの移動平均のピリオド。 |
| `LongAppliedPrice`, `ShortAppliedPrice` | 各ブロックに使用する価格ソース（終値、始値、典型値、Demark等）。 |
| `EnableLongEntries`, `EnableShortEntries` | 新しいロング/ショートポジションを切り替え。 |
| `EnableLongExits`, `EnableShortExits` | 自動エグジットを切り替え。 |
| `LongSignalBar`, `ShortSignalBar` | クロスに評価する歴史的シフト（バー遡り）。 |
| `LongReverse`, `ShortReverse` | 比率を反転（MQLの`Revers`フラグを反映）。 |
| `StopLossPoints`, `TakeProfitPoints` | 価格ステップで表現した保護距離（無効化するにはゼロに設定）。 |
| `Volume`（ベースストラテジーのプロパティ） | デフォルトのトレードサイズを定義。 |

## 実装メモ
- 移動平均はStockSharpのインジケーターライブラリから取得します。高度なスムーザー（`JJMA`、`JurX`、`ParMA`、`VIDYA`）は直接の同等物が利用できないため、デフォルトで指数スムージングを使用します。
- インジケーター値は完成したローソク足でのみ処理され、元の`IsNewBar`動作に対応します。
- シグナルキューは必要な数の歴史的比率値のみを維持し、重いコレクションを避けます。
- 保護ストップはオプションです；両方の距離がゼロの場合でも、ストラテジーはフレームワークのライフサイクルに準拠するために`StartProtection()`を呼び出します。

## 使用上のヒント
- ローソク足タイプをコネクターで利用可能なデータサブスクリプションに合わせてください。
- 取引するインスツルメントに合わせて移動平均の長さと適用価格を最適化してください。
- 非対称な時間軸（ロング/ショート）を使用する場合、両方のサブスクリプションは明確さのために別々のチャートエリアに表示されます。

## MQL5バージョンとの比較における制限
- 資金管理モード（`MM`、`MarginMode`）は再現されていません；トレードサイズはStockSharpの`Volume`プロパティに従います。
- `SmoothAlgorithms.mqh`の一部のエキゾチックなスムージングアルゴリズムは指数移動平均で近似されます。
- ストップ/リミット注文は個別の注文パラメーターではなく、汎用的な保護レベルに変換されます。
