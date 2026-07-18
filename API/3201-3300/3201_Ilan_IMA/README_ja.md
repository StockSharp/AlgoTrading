# Ilan iMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**Ilan iMA戦略**は、MetaTrader 5のエキスパートアドバイザー`Ilan iMA.mq5`のStockSharp移植版です。このアドバイザーは、シフトされた移動平均のトレンドフィルターとマーティンゲールスタイルの平均化グリッドを組み合わせています。StockSharpバージョンは高水準APIで同じアイデアを再実装しています：加重移動平均がトレンドを確認すると、戦略はマーケット注文を開き、価格が設定可能なステップだけポジションに逆行するたびに取引を追加し続けます。利益目標、トレーリングストップ、または明示的なストップロスに達すると、バスケット全体が閉じられ、元のEAのマネー管理モデルを再現します。

## トレーディングロジック
1. 選択されたタイムフレーム（`CandleType`）を購読し、設定可能な移動平均（`MaMethod`、`MaPeriod`、`PriceMode`）をフィードします。正の`MaShift`はインジケーターを前にシフトするので、戦略はMT5の動作を模倣するために過去の値を評価します。
2. ローソク足のクローズを待ちます。完了したバーのみがシグナルを生成し、トレーリング/ストップロジックを更新します。
3. `MaShift`バーでシフトされた4つの連続した移動平均値を比較してトレンドを検出します：
   - 厳密に減少する値は下降トレンドを示します；
   - 厳密に増加する値は上昇トレンドを示します。
4. バスケットが開いていない場合：
   - 下降トレンドでクローズが移動平均値を上回っている場合、`StartVolume`でショートを開きます；
   - 上昇トレンドでクローズが移動平均値を下回っている場合、`StartVolume`でロングを開きます。
5. バスケットが存在する場合：
   - 価格がポジションに対して少なくとも`GridStepPips`動いた場合、サイズが`LotExponent`で成長するが`LotMaximum`と取引所ボリューム制限で制限される別の注文を開きます；
   - 平均エントリー価格、最低買い価格、最高売り価格はMT5ロジックに近い動作を維持するために内部的に追跡されます。
6. クローズ条件：
   - 複数の取引を持つバスケットの浮動利益が`ProfitMinimum`（口座通貨）に達すると、その方向のすべての注文を閉じます；
   - 浮動利益が`TakeProfitPips`に達するか損失が`StopLossPips`に達すると、バスケットを閉じます；
   - トレーリング保護は`TrailingStopPips + TrailingStepPips`ポイントの有利な動きの後に有効になり、`TrailingStepPips`のステップで動きます。

## リスク管理とサイジング
- `StartVolume`はMT5の`StartLots`パラメーターを再現します。各追加注文は`LotMaximum`と会場の制限（`Security.MinVolume`、`Security.VolumeStep`、`Security.MaxVolume`）を尊重しながら前のサイズを`LotExponent`倍にします。
- `ProfitMinimum`はMT5バージョンの「ロック解除」動作を保持します：グリッドがヘッジから回復して要求された利益を印刷すると、その方向のすべての取引が閉じられます。
- ストップロスとテイクプロフィット距離はpipで測定されます（`StopLossPips`、`TakeProfitPips`）。ヘルパーメソッドは`Security.PriceStep`を使用してpipを取引所価格ステップに変換します。
- トレーリングブロックはMT5実装を模倣します：トレーリングは価格が`TrailingStopPips + TrailingStepPips`を超えた後にのみ始まり、早期のストップ調整を避けるために離散ステップで更新されます。

## パラメーター
| 名前 | 型 | デフォルト | MT5対応 | 説明 |
| --- | --- | --- | --- | --- |
| `MaPeriod` | `int` | `15` | `Inp_MA_ma_period` | トレンドフィルター移動平均の期間。 |
| `MaShift` | `int` | `5` | `Inp_MA_ma_shift` | バーでの移動平均線の前方シフト。 |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | `Inp_MA_ma_method` | 平滑化アルゴリズム（SMA、EMA、SMMA、LWMA）。 |
| `PriceMode` | `CandlePrice` | `Weighted` | `Inp_MA_applied_price` | インジケーターに投入するローソク足の価格。 |
| `StartVolume` | `decimal` | `1` | `InpStartLots` | バスケットの最初の取引のベース注文ボリューム。 |
| `GridStepPips` | `decimal` | `30` | `InpStep` | 平均化エントリー間の距離（pip単位）。 |
| `LotExponent` | `decimal` | `1.6` | `InpLotExponent` | 前の注文サイズに適用される乗数。 |
| `LotMaximum` | `decimal` | `15` | `InpLotMaximum` | 単一注文ボリュームのハード上限。 |
| `ProfitMinimum` | `decimal` | `15` | `InpProfitMinimum` | 複数の取引を持つバスケットを閉じるために必要な最小浮動利益。 |
| `StopLossPips` | `decimal` | `0` | `InpStopLoss` | pipで表したストップロス距離（0でストップを無効化）。 |
| `TakeProfitPips` | `decimal` | `100` | `InpTakeProfit` | pipで表したテイクプロフィット距離。 |
| `TrailingStopPips` | `decimal` | `15` | `InpTrailingStop` | トレーリングストップを有効にする利益閾値。 |
| `TrailingStepPips` | `decimal` | `5` | `InpTrailingStep` | トレーリングストップが再び動く前の最小追加利益。 |
| `CandleType` | `DataType` | 15分足 | チャート期間 | シグナル計算に使用するタイムフレーム。 |

## 元のEAとの違い
- StockSharpはネッティング環境で動作するため、方向ごとに1つのネットポジションのみが存在します。戦略はMT5のバスケット会計を模倣するためにエントリー価格とボリュームの内部リストを保持します。
- 取引所固有のボリューム制限はボリュームを丸める際に常に尊重されますが、MT5コードは手動チェックに依存していました。これによりブローカーコネクターによって拒否される注文を防ぎます。
- ストップロス、テイクプロフィット、トレーリングロジックは既存のMT5ポジションを変更するのではなく、マーケット出口を通じて表現されます。機能的な動作は同じですが、注文管理はStockSharpによって処理されます。

## 使用上の注意
- pipから価格への変換とボリュームの丸めが正しく機能するように、コネクターにインストゥルメントのメタデータ（`PriceStep`、`StepPrice`、`MinVolume`、`VolumeStep`、`MaxVolume`）が入力されていることを確認してください。
- トレーリングブロックはpipサイズが取引所価格ステップと等しいと仮定します。非従来型のティックサイズを持つインストゥルメントには`GridStepPips`、`StopLossPips`、`TrailingStopPips`を調整してください。
- マーティンゲールグリッドは本質的にリスクが高いです。本番環境に展開する前に歴史データで戦略をテストし、現実的な手数料/スリッページ設定を使用してください。
