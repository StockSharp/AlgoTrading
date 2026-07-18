# Exp Iin MA Signal MMRec 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MetaTrader エキスパート「Exp_Iin_MA_Signal_MMRec」のStockSharpポート。戦略は設定可能な移動平均のペア（元のIin_MA_Signalインジケーター）が生成するクロスオーバーシグナルをリッスンし、損失ベースの削減を伴う適応的なポジションサイジングスキームを適用します。

## 概要

- **シグナル生成**: 高速と低速の移動平均が選択したキャンドルタイプと適用価格で評価されます。高速平均が低速を上回ったときに買いシグナルが生成され、反対のクロスオーバーで売りシグナルが生成されます。`SignalBar`パラメーターは指定された数の完全にクローズされたバー分だけ実行を延期し、MQLバージョンで使用されるインジケーターバッファーのラグを再現します。
- **ポジション管理**: `BuyPosOpen`と`SellPosOpen`はロングとショートエントリーを有効または無効にします。反対のシグナルが現れ、対応する`BuyPosClose`または`SellPosClose`フラグが有効な場合、戦略は現在のエクスポージャーをクローズするか、直接新しい方向に反転します。
- **リスク制御**: `StopLossPoints`と`TakeProfitPoints`は`Security.PriceStep`を使用して価格距離に変換され、新しいシグナルを処理する前にキャンドルの極値と照合されます。
- **資金管理**: 最後の取引はロングとショートで別々に追跡されます。`BuyTotalTrigger`/`SellTotalTrigger`ウィンドウ内の損失取引数がそれぞれの損失しきい値に達すると、戦略は`NormalVolume`から`ReducedVolume`に切り替わります。`MoneyMode`パラメーターはボリューム値の解釈方法を定義します（固定ロット、残高パーセンテージ、またはstopベースのリスクパーセンテージ）。

## パラメーター

- `FastPeriod`, `SlowPeriod` – 高速と低速移動平均の長さ。
- `FastType`, `SlowType` – 移動平均のタイプ（`Simple`、`Exponential`、`Smoothed`、`Weighted`、`VolumeWeighted`）。
- `FastPrice`, `SlowPrice` – 各平均の適用価格（`Close`、`Open`、`High`、`Low`、`Median`、`Typical`、`Weighted`）。
- `SignalBar` – 検出されたシグナルと注文提出の間のクローズバーの数。
- `BuyPosOpen`, `SellPosOpen` – ロング/ショートポジションを開くためのトグル。
- `BuyPosClose`, `SellPosClose` – 反対シグナルで既存ポジションをクローズまたは反転するためのトグル。
- `BuyTotalTrigger`, `SellTotalTrigger` – 損失カウンターのために検査される最近の取引数。
- `BuyLossTrigger`, `SellLossTrigger` – 削減ボリュームを有効にする検査ウィンドウ内の最小損失数。
- `NormalVolume`, `ReducedVolume` – プライマリとフォールバックボリューム（または`MoneyMode`に応じたリスクファクター）。
- `StopLossPoints`, `TakeProfitPoints` – インストゥルメントポイントでのストップロスとテイクプロフィット距離。
- `MoneyMode` – ボリューム値の解釈（`Lot`、`Balance`、`FreeMargin`、`BalanceRisk`、`FreeMarginRisk`）。残高ベースのモードは`Portfolio.CurrentValue`を使用し、リスクベースのモードはリスク金額をstop距離で割ります。
- `CandleType` – インジケーター計算に使用されるキャンドルシリーズ。

## シグナルロジック

1. すべての完了したキャンドルが選択した適用価格で移動平均に供給されます。
2. 移動平均の現在値と前の値の差がクロスオーバーイベントを定義します。
3. シグナルはキューに入れられ、キューサイズが`SignalBar`を超えると最も古いエントリーが実行されます。
4. 買いシグナルが実行されると：
   - ショートポジションが存在し`SellPosClose`が有効な場合、戦略はそのショートトレードの実現PnLを計算します。次に（`BuyPosOpen`が有効な場合）ロングに反転するか、単にエクスポージャーをクローズします。
   - ポジションが開いておらず`BuyPosOpen`が有効な場合、計算されたボリュームで新しいロングが開かれます。
5. 売りシグナルは買いのワークフローを反映します。

## 資金管理の詳細

- 取引履歴は`BuyTotalTrigger` / `SellTotalTrigger`によって制限されるローリングFIFOキューとして保存されます。
- 損失取引（負のPnL）は損失カウンターを増加させます。カウンターが`BuyLossTrigger`または`SellLossTrigger`に達すると、次のポジションは`ReducedVolume`を使用します。
- `MoneyMode = Lot`はボリューム値を生の量として扱います。
- `MoneyMode = Balance`と`FreeMargin`は設定値に`Portfolio.CurrentValue`を掛けて現在のクローズ価格で割り、数量を取得します。
- `MoneyMode = BalanceRisk`と`FreeMarginRisk`は設定値に`Portfolio.CurrentValue`を掛けてストップロス距離で割ります。stop距離がゼロの場合、フォールバックは残高パーセンテージ計算と同一です。
- ポートフォリオ情報が利用できない場合、計算されたボリュームはデフォルトでゼロになり、偶発的な注文を避けます。

## リスク処理

- ストップロスとテイクプロフィットレベルはエントリー価格とポイント値を使用してすべてのキャンドルで再計算されます。レベルがキャンドル範囲内に触れた場合、新しいシグナルが処理される前にポジションがクローズされます。
- クローズアクションは常に取引結果を記録し、資金管理キューが実際のエグジットと同期したままであることを保証します。

## 注意事項

- `StopLossPoints`と`TakeProfitPoints`がインストゥルメントのティックサイズと互換性があることを確認してください；戦略は`Security.PriceStep`でそれらを乗算します。
- `MoneyMode`がポートフォリオデータに依存する場合、戦略は`Portfolio`オブジェクトが`CurrentValue`を公開することを期待します。
- アルゴリズムはネットポジション基準で動作します：同時のロングとショートの保有はサポートされていません。
