# WAMI クラウド X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は元のMetaTraderエキスパートアドバイザー「Exp_WAMI_Cloud_X2」のデュアル時間軸動作を複製します。上位時間軸でWarren Momentum Indicator (WAMI)を使用して支配的なバイアスを定義し、同じインジケーターの2番目のインスタンスを下位時間軸でエントリーとエグジットのタイミングに使用します。メインWAMIラインは両方の時間軸でその内部シグナルラインと比較され、元のMQL実装のロジックを反映します。

## コンセプト

- **WAMI構築** – WAMIは終値の一次差分から構築され、個別に選択可能な方法（SMA、EMA、SMMA、またはLWMA）で3つの連続した移動平均でスムージングされます。4番目の移動平均がシグナルラインを生成します。戦略のカスタムインジケーターはこのチェーンを正確に再現し、メインラインとシグナルラインの両方が1つの値ペイロードで利用可能です。
- **トレンドフィルター（上位時間軸）** – デフォルトの6時間足がトレンドWAMIを駆動します。メインラインがシグナルラインより上にある場合、トレンド方向は強気になります。下にある場合は弱気になります。両方のラインが等しい場合またはインジケーターがまだ形成中の場合は中立の状態が維持されます。
- **シグナルエンジン（下位時間軸）** – デフォルトの30分足がエントリーを探すために使用されます。各完成したキャンドルごとに、戦略は最近のWAMI値を保存し、`SignalBar`で定義された最後の閉じたバーを評価します。クロスは最新の値（`SignalBar`）と前の値（`SignalBar + 1`）を比較することで検出されます。

## トレーディングルール

1. **エグジット**
   - `CloseLongOnSignal`が有効な場合、シグナル時間軸が持続的な弱気を示す（`previous.Main < previous.Signal`）とロングポジションが閉じられます。
   - `CloseShortOnSignal`が有効な場合、ショートポジションは同様に閉じられます。
   - 上位時間軸の方向が変わる（`_trendDirection`）と、それぞれの`CloseLongOnTrendFlip`または`CloseShortOnTrendFlip`フラグが強制的にエグジットさせます。
2. **エントリー**
   - 上位時間軸が弱気でシグナルWAMIが上向きにクロスする（`current.Main >= current.Signal`かつ`previous.Main < previous.Signal`）場合にショートエントリーが許可されます。これは下降トレンド内でシグナルラインへの最初の上向き突破で売る元のEAと一致します。
   - ロングエントリーは、上位時間軸が強気でシグナルWAMIが下向きにクロスする（`current.Main <= current.Signal`かつ`previous.Main > previous.Signal`）場合の鏡像条件です。
   - エントリートグル（`EnableBuyEntries`、`EnableSellEntries`）はどちらのサイドも無効化できます。反対のポジションが開いている場合、戦略はMQLヘルパー関数と同様に1つのコマンドでフラット化および反転するための補償成行注文を送信します。

## パラメーター

- **トレンドWAMI** – `TrendPeriod1/2/3`、`TrendMethod1/2/3`、`TrendSignalPeriod`、`TrendSignalMethod`、`TrendCandleType`。
- **シグナルWAMI** – `SignalPeriod1/2/3`、`SignalMethod1/2/3`、`SignalSignalPeriod`、`SignalSignalMethod`、`SignalCandleType`。
- **コントロールフラグ** – `SignalBar`、`EnableBuyEntries`、`EnableSellEntries`、`CloseLongOnTrendFlip`、`CloseShortOnTrendFlip`、`CloseLongOnSignal`、`CloseShortOnSignal`。
- **トレーディングサイズ** – `TradeVolume`は新規エントリーに使用される成行注文サイズを定義します。反転では逆のボリュームと設定サイズが送信されます。

すべてのパラメーターは`StrategyParam<T>`オブジェクトを通じて公開されており、MetaTraderの入力が許可していたのと同じようにStockSharp UIから最適化または変更できます。

## デフォルト値

- **トレンド時間軸** – 6時間足。
- **シグナル時間軸** – 30分足。
- **すべての移動平均方法** – 単純（SMA）。
- **移動平均の長さ** – 3段階で4 / 13 / 13、両方の時間軸のシグナルラインで4。
- **SignalBar** – 1（最後の閉じたキャンドルを使用）。
- **TradeVolume** – 1コントラクト。
- **すべての許可フラグ** – 有効（true）。

## 追加注記

- 戦略はハードなストップロスまたはテイクプロフィット注文を設定しません。必要な場合はリスク管理を外部で設定する必要があります。
- チャートヘルパーはシグナル時間軸のキャンドル、両方のWAMIライン、および実行されたトレードを描画します。トレンド時間軸は視覚的な確認のために別のエリアにプロットされます。
- 実装はインジケーター値のポーリングを避け（`GetValue`呼び出しなし）、プロジェクトガイドラインに従って高レベルのキャンドルサブスクリプションAPIに固執します。
