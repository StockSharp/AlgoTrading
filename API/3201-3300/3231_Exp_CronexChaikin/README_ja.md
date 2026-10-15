# Exp Cronex Chaikin戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTraderエキスパートアドバイザー**Exp_CronexChaikin.mq5**をStockSharpの高レベルAPIにポートします。元のロボットは累積/分散値からChaikinオシレーターを再構築し、Cronex「XMA」フィルターで2回平滑化し、高速線と低速線のクロスオーバーで取引します。StockSharpバージョンは同じロジックを再現しながら、各段階を設定可能なパラメーターとして公開しています。

## トレーディングロジック

1. 設定されたローソク足シリーズ（`CandleType`）をサブスクライブします。
2. 選択された`VolumeSource`（ティックまたは実際のボリューム）を使用して、完了した各ローソク足の累積/分散（AD）ラインを再計算します。
3. AD ラインを2つの移動平均（`ChaikinFastPeriod`、`ChaikinSlowPeriod`、`ChaikinMethod`）で平滑化し、その差を取ることでChaikinオシレーターを適用します。
4. `SmoothingMethod`、`FastPeriod`、`SlowPeriod`、`Phase`で制御されるCronexフィルターを使用して、結果のオシレーターを2回平滑化します。これら2つの平滑化された値は、元のインジケーターの「高速」線と「シグナル」線に対応します。
5. `SignalBar`個の完了したローソク足を遡り、その棒と前の棒の両方のCronexラインを比較します。
6. 高速線が低速線より上にある場合、戦略はオプションでショートポジションを決済し、`BuyOpenEnabled`がtrueの場合、ルックバックバーで新鮮な上昇クロスが検出されたらロングポジションを開きます。
7. 高速線が低速線より下にある場合、`SellOpenEnabled`と`BuyCloseEnabled`で制御されるショート取引に対して逆のアクションが実行されます。
8. 新しいポジションが開かれるたびに、ストップロスとテイクプロフィット注文（ポイントで表現）が`StopLoss`と`TakeProfit`で再計算されます。

単一のネットポジションのみが維持されます。シグナルの方向が変わると、戦略は現在のポジションを決済するために必要なボリュームと新しい取引サイズを組み合わせてMetaTraderのネッティング動作を模倣します。

## インジケーターと平滑化オプション

- **Chaikinオシレーター**: 選択された`ChaikinMethod`移動平均タイプを累積/分散ラインに適用して構築されます。利用可能なオプションには、単純、指数、平滑化、線形加重平均が含まれます。
- **Cronexスムーザー**: `SmoothingMethod`パラメーターはCronex XMAファミリー（SMA、EMA、SMMA、LWMA、Jurik JJMA/JurX、Parabolic MA、T3、VIDYA、AMA）を公開します。`Phase`パラメーターはMQL実装と全く同じようにJurikベースのフィルターに影響します。

## パラメーター

| パラメーター | 説明 |
|-----------|-------------|
| `CandleType` | インジケーターの計算に使用するローソク足のデータタイプ。デフォルトは4時間時間軸です。 |
| `ChaikinMethod` | Chaikinオシレーター内で使用される移動平均手法。 |
| `ChaikinFastPeriod` / `ChaikinSlowPeriod` | 累積/分散ラインに適用される高速・低速期間。 |
| `SmoothingMethod` | Chaikinオシレーター値に適用されるCronex平滑化アルゴリズム。 |
| `FastPeriod` / `SlowPeriod` | 高速・低速Cronexラインの長さ。 |
| `Phase` | Jurikベースのスムーザーの位相パラメーター（範囲-100〜+100）。 |
| `VolumeSource` | 累積/分散ラインの計算時にティックまたは実際のボリュームを選択。 |
| `SignalBar` | クロスオーバーシグナルを含む必要がある完了バーの遡り数。 |
| `BuyOpenEnabled` / `SellOpenEnabled` | ロングまたはショート取引の開始を有効または無効にします。 |
| `BuyCloseEnabled` / `SellCloseEnabled` | 逆シグナル時に反対ポジションの決済を許可します。 |
| `TakeProfit` / `StopLoss` | 各エントリー後に適用される計器ポイントでの利益目標と保護ストップ距離。 |
| `Volume` | 標準StockSharpポジションサイズ（元のエキスパートのロットサイズとして機能）。 |

## MQLバージョンとの違い

- `TradeAlgorithms.mqh`からのマネー管理とスリッページルーティンは、組み込みの`Volume`、`SetStopLoss`、`SetTakeProfit`ヘルパーに置き換えられます。
- StockSharp実装は完了したローソク足でのみADラインを再計算し、テストとライブ取引の決定論的動作を確保します。
- Cronex平滑化オプションはStockSharpインジケーターに依存します：JurikフィルターはJurikMovingAverage（フェーズ制御付き）でサポートされ、VIDYAとParMAは他のCronex変換と一致した指数近似を使用します。
