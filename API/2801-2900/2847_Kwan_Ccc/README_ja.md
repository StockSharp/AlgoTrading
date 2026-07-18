# KWAN CCC戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
KWAN CCC戦略は、StockSharpの高レベルAPIを使用してMetaTraderエキスパート`Exp_KWAN_CCC.mq5`を再現します。このシステムは、以下のように構築されたカスタムオシレーターからトレーディングシグナルを導出します：

1. Chaikinオシレーターを計算します（累積/分配ラインの高速と低速移動平均の差）。
2. ChaikinのValueをCommodity Channel Index（CCI）で乗算します。
3. 結果をMomentumインジケーターの値で除算します。Momentumがゼロの場合、スクリプトはゼロ除算を避けるために定数値100を代入します — オリジナルコードと全く同じです。
4. 結果の系列をユーザーが選択したXMAメソッドで平滑化します。
5. 平滑化された系列の傾きを検出します。上昇バーは`0`、下降バーは`2`、それ以外は`1`で色付けされます。

色が`0`から他の値に変わると、戦略はショートを閉じてロングポジションを開きます。色が`2`から他の値に変わると、ロングを閉じてショートを開きます。これはオプションのシグナルシフト（`SignalBar`）を含むMQLエキスパートに実装されたロジックを反映しています。

## トレーディングルール
- **ロングエントリー**: `SignalBar + 1`のバーの色が`0`に等しく、`SignalBar`のバーが`0`と異なる。
- **ショートエントリー**: `SignalBar + 1`のバーの色が`2`に等しく、`SignalBar`のバーが`2`と異なる。
- **ロングエグジット**: `EnableLongExits = true`でショートエントリー条件がトリガーされた場合に有効。
- **ショートエグジット**: `EnableShortExits = true`でロングエントリー条件がトリガーされた場合に有効。
- 保護ストップとターゲット注文は、`StopLossPoints`と`TakeProfitPoints`に楽器の`PriceStep`を掛けた絶対的な価格オフセットを使用して`StartProtection`を通じて作成されます。

## パラメーター
| パラメーター | 説明 |
|-------------|------|
| `OrderVolume` | 新しいポジションを開く際に使用する基本注文サイズ。 |
| `CandleType` | すべてのインジケーター計算の時間軸。デフォルトは1時間。 |
| `FastPeriod` / `SlowPeriod` | Chaikinオシレーター内の移動平均の長さ。 |
| `ChaikinMethod` | 累積/分配ラインに適用する移動平均タイプ（単純、指数、平滑、加重）。 |
| `CciPeriod` | Commodity Channel Indexのピリオド。 |
| `MomentumPeriod` | Momentumインジケーターのピリオド。 |
| `SmoothingMethod` | オリジナルオプションからマッピングされたXMA平滑化メソッド。`JurX`、`Parabolic`、`T3`はJurik MAにフォールバック；`Vidya`はChande Momentumオシレーター駆動の適応型平滑化を使用；`Adaptive`はKaufman AMAを使用。 |
| `SmoothingLength` | 選択した平滑化フィルターで使用するバーの数。 |
| `SmoothingPhase` | 特定のメソッドで使用される追加パラメーター（例：VIDYA CMO長、AMAスロー期間）。 |
| `SignalBar` | 色の遷移を評価するために使用するオフセット（完了したバーで）。`1`はMetaTraderのデフォルトを再現。 |
| `EnableLongEntries` / `EnableShortEntries` | 対応する方向への新しいポジションの開設を許可または禁止。 |
| `EnableLongExits` / `EnableShortExits` | インジケーター駆動のポジション決済を許可または禁止。 |
| `StopLossPoints` / `TakeProfitPoints` | 価格ステップで測定した保護ストップ/ターゲット（無効化するにはゼロに設定）。 |

## 実装ノート
- 戦略は完成したローソク足にのみ作用し、StockSharpの`Bind`ヘルパーを使用してローソク足データをインジケーターにストリームします。
- 平滑化メソッドリストはオリジナルライブラリのXMA実装を反映しています。StockSharpで利用できないメソッドは最も近い代替にマッピングされ、パラメーターテーブルに記載されています。
- MetaTraderの`VolumeType`入力は省略されています。StockSharpのローソク足は既に累積/分配ラインで使用される総ボリューム情報をカプセル化しているためです。
- オリジナルエキスパートのマネー管理はカスタムロットサイジングヘルパーに依存していました。変換では`OrderVolume`で指定された固定ボリュームを前提としています。

## 使用上のヒント
- Chaikinオシレーターの動作が重要な場合は、インストゥルメントが意味のあるボリュームデータを提供していることを確認してください。流動性の低いインストゥルメントの場合は、ノイズを減らすために`MomentumPeriod`を増やすことを検討してください。
- 平滑化パラメーターを最適化する際は、`SmoothingLength`と`SmoothingPhase`を慎重に組み合わせてください：極端な組み合わせはシグナルをかなり遅延させる可能性があります。
- デフォルトの保護値（`StopLossPoints = 1000`、`TakeProfitPoints = 2000`）は大きなオフセットに対応しています。インストゥルメントのティックサイズに合わせて調整してください。
