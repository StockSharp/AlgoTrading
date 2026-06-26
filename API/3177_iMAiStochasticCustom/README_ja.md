# iMA iStochastic Custom戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略はMetaTraderエキスパート **「iMA iStochastic Custom」** をStockSharpフレームワーク内で再現します。移動平均エンベロープとステキャスティクオシレーターフィルターを組み合わせます。取引は選択した時間軸（`CandleType`）の完成したローソク足で行われます。以下のすべてのコメントはオリジナルのアドバイザーと同じ命名を使用します。

主要コンポーネント：

1. **移動平均エンベロープ** – ベースの移動平均が `LevelUpPips` と `LevelDownPips`（pips で表示）でシフトされ、レジスタンスとサポートバンドを構築します。平均化メソッドはMetaTraderのオプションに対応します：単純（SMA）、指数（EMA）、平滑化（SMMA）、線形加重（LWMA）。
2. **ステキャスティクオシレーター** – %K、%D、平滑化の長さはオリジナルのパラメーターに従います。2つのしきい値（`StochasticLevel1` と `StochasticLevel2`）が買われすぎ/売られすぎの条件を検証します。
3. **マネーマネジメント** – オリジナルの `lot`/`risk` セレクターは `ManagementMode` パラメーターを通じて保持されます。`FixedLot` モードでは注文サイズは `VolumeValue` と等しくなります。`RiskPercent` モードでは、戦略はストップロス距離に対してポートフォリオ資産の設定されたパーセンテージをリスクにさらし、`CMoneyFixedMargin` の動作を再現します。
4. **保護** – ストップロス、テイクプロフィット、トレーリングの距離はpipsで入力されます。トレーリングは完成したローソク足で更新され、StockSharpのイベントモデルと互換性を保ちながらMQLロジックを反映します。

## トレードロジック
ロングとショートのシグナルは対称的です：

- **買い** ローソク足の終値が上部エンベロープ（`ma + LevelUpPips`）を上回り、ステキャスティクのいずれかのラインが `StochasticLevel1` を上回っている場合。
- **売り** ローソク足の終値が下部エンベロープ（`ma + LevelDownPips`）を下回り、ステキャスティクのいずれかのラインが `StochasticLevel2` を下回っている場合。
- `ReverseSignals = true` を設定するとエントリー方向が入れ替わります。

一度にアクティブなネットポジションは1つだけです。シグナルが反転すると、戦略は現在のエクスポージャーを均し、反対方向に新しいポジションを開くのに十分な大きさの注文を送信します。

## リスクコントロールとエグジット
- **ストップロス / テイクプロフィット** – インストゥルメントの `PriceStep` を通じて変換されたpips距離。各完成したローソク足の高値/安値を使用して確認されます。
- **トレーリングストップ** – 価格がポジションに有利な方向に `TrailingStopPips` 動いた後に開始します。MQLトレーリングルーティンと同様に、各調整前に `TrailingStepPips` の追加改善を必要とします。
- **マネーマネジメント** – リスクモードでのポジションサイズは `equity * VolumeValue / 100 / perUnitRisk` であり、`perUnitRisk` はストップロスまでの1ロット当たりの金銭的損失です。

## パラメーター
| パラメーター | 説明 |
|-----------|------|
| `CandleType` | 計算に使用する時間軸。 |
| `StopLossPips`, `TakeProfitPips` | pipsでの保護距離。 |
| `TrailingStopPips`, `TrailingStepPips` | トレーリングの有効化とステップ（pips）。無効にするにはゼロを設定。 |
| `ManagementMode`, `VolumeValue` | 固定ロットまたはリスク割合のサイジング。 |
| `MaPeriod`, `MaShift`, `MaMethod` | 移動平均の長さ、バーシフト、メソッド（SMA/EMA/SMMA/LWMA）。 |
| `LevelUpPips`, `LevelDownPips` | pipsでの上部/下部エンベロープオフセット。下部バンドでは負の値も使用可能。 |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | オシレーター設定。 |
| `StochasticLevel1`, `StochasticLevel2` | 買い/売り確認のための確認レベル。 |
| `ReverseSignals` | すべての取引の方向を反転する。 |

## 実装ノート
- ローソク足、インジケーター、注文は高レベルAPI（`SubscribeCandles().BindEx(...)`）を通じて接続されます。
- pip サイズは必要に応じて `PriceStep` を乗算することで3/5桁のフォレックスシンボルに自動的に調整されます。
- トレーリングロジックは完成したローソク足で動作します。バー内トレーリングが必要な場合は、ティックレベルデータにロジックをフックしてください。
- Pythonポートは提供されていません；要求通り `PY` フォルダーは意図的に存在しません。

## MetaTraderバージョンとの違い
- リスクサイジングは明示的であり、`CMoneyFixedMargin` ヘルパークラスではなくStockSharpポートフォリオメトリクスに基づいています。ストップロスが有効な場合の結果ロットはオリジナルの動作と一致します；ストップロスがゼロの場合、ポジションサイズはゼロのままであり、MQLの保護を反映します。
- 保護チェック（ストップロス、テイクプロフィット、トレーリング）はStockSharp戦略がイベント駆動であるため完成したローソク足で評価されます。これにより論理が確定的に保たれ、バックテストの制約に対応します。
- ロギングはStockSharpの標準出力に簡略化されています；詳細な `InpPrintLog` フラグは継承されません。

この戦略はMetaTraderからStockSharp DesignerまたはRunnerに移行する際の直接の代替として使用してください。ターゲットのインストゥルメントと時間軸に合わせてパラメーターを調整してください。
