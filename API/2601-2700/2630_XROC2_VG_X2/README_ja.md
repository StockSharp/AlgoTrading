# XROC2 VG X2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
XROC2 VG X2戦略は、2本の平滑化された価格変化率ストリームを組み合わせたマルチ時間軸システムです。上位時間軸が方向性フィルターとして機能し、下位時間軸が具体的なエントリーおよびエグジットシグナルを生成します。元のMetaTrader 5エキスパートアドバイザーは、柔軟な平滑化オプションとマネー管理モジュールを持つカスタムXROC2_VGインジケーターに依存していました。StockSharpポートはシグナルロジックをそのまま維持し、主要なパラメーターを戦略入力として公開します。

戦略は2つのローソク足シリーズにサブスクライブします：
- **上位時間軸**（デフォルト6時間）– 支配的なトレンド方向を確立します。
- **下位時間軸**（デフォルト30分）– 2本の平滑化されたROC線がどのように交差するかを監視してエントリーとエグジットを生成します。

両ストリームは同じ価格変化率計算モードを共有しますが、個別の平滑化設定を使用します。デフォルトでは戦略はJurik移動平均を適用し、MQLバージョンを模倣します。StockSharpが直接サポートしていない高度な平滑化タイプ（JurX、ParMA、T3、VIDYA、フェーズ制御付きAMA）は、最も近い利用可能な移動平均実装にフォールバックします。

## トレードロジック
1. **トレンド検出（上位時間軸）**
   - 設定された期間と平滑化方法を使用して2つの平滑化ROC値を計算する。
   - `HigherSignalBar`で定義されたバーでラインペアを評価する。速い線が遅い線より上にあればトレンドは強気、そうでなければ弱気。中立の読みは現在のトレンドをゼロに保ちトレーディングを無効にする。
2. **シグナル生成（下位時間軸）**
   - 下位時間軸で同じ平滑化ROC値のペアを計算する。
   - 最新の完成バー（シフト`LowerSignalBar`）とその前のバーを見る。この2本のバーの組み合わせが直前にクロスが発生したかどうかを決定する。
   - ロングセットアップは上位時間軸が強気で、速い線が遅い線を下方にクロスし（下方クロス）、ロングが有効な場合に現れる。
   - ショートセットアップは上位時間軸が弱気で、速い線が遅い線を上方にクロスし（上方クロス）、ショートが有効な場合に現れる。
3. **ポジション管理**
   - 下位時間軸のクロスが弱気を示すとき（`CloseBuyOnLower`）または上位時間軸のトレンドが弱気に転換するとき（`CloseBuyOnTrendFlip`）にロングポジションを閉じる。
   - 下位時間軸のクロスが強気になるとき（`CloseSellOnLower`）または上位時間軸のトレンドが強気に転換するとき（`CloseSellOnTrendFlip`）にショートポジションを閉じる。
   - 新規トレードはアクティブなポジションがない場合にのみ開かれる。注文サイズは戦略の`Volume`プロパティで制御される。

## パラメーター
- `HigherCandleType` – トレンドフィルター用のローソク足タイプ（デフォルト6時間足）。
- `LowerCandleType` – シグナル生成用のローソク足タイプ（デフォルト30分足）。
- `HigherSignalBar` – 上位時間軸の値を読み取る際にシフトする閉じたバーの数（デフォルト1）。
- `LowerSignalBar` – 下位時間軸の値を読み取る際にシフトする閉じたバーの数（デフォルト1）。
- `HigherRocMode` / `LowerRocMode` – 価格変化率計算バリアント（`Momentum`、`RateOfChange`、`RateOfChangePercent`、`RateOfChangeRatio`、`RateOfChangeRatioPercent`）。
- `HigherFastPeriod`, `HigherFastMethod`, `HigherFastLength`, `HigherFastPhase` – 上位時間軸の高速ROC設定。
- `HigherSlowPeriod`, `HigherSlowMethod`, `HigherSlowLength`, `HigherSlowPhase` – 上位時間軸の低速ROC設定。
- `LowerFastPeriod`, `LowerFastMethod`, `LowerFastLength`, `LowerFastPhase` – 下位時間軸の高速ROC設定。
- `LowerSlowPeriod`, `LowerSlowMethod`, `LowerSlowLength`, `LowerSlowPhase` – 下位時間軸の低速ROC設定。
- `AllowBuyOpen`, `AllowSellOpen` – ロングおよびショートの開設を有効/無効にする。
- `CloseBuyOnTrendFlip`, `CloseSellOnTrendFlip` – 上位時間軸が方向を変えるときに強制終了する。
- `CloseBuyOnLower`, `CloseSellOnLower` – 下位時間軸のクロスがポジションに反する場合に終了する。

## 実装上の注意
- 元のMQL戦略は大規模な平滑化ライブラリを使用していました。StockSharpバージョンはサポートされているオプションを組み込みインジケーター（SMA、EMA、SMMA/RMA、LWMA、Jurik、Kaufman AMA）にマッピングします。サポートされていないモード（JurX、ParMA、T3、VIDYA）は最も近い利用可能な移動平均で近似されるため、これらの組み合わせでは動作が異なる場合があります。
- `TradeAlgorithms.mqh`からのマネー管理機能、ストップロス、テイクプロフィット、スリッページ設定は再現されていません。代わりに、戦略設定で指定された固定`Volume`でトレードします。
- 注文は成行注文で実行されます。必要に応じてStockSharpの保護モジュールを通じてストップロスやトレーリングストップなどの保護ロジックを追加できます。
- 戦略は両方のローソク足サブスクリプションが完全に形成され、`IsFormedAndOnlineAndAllowTrading()`が真を返す場合にのみトレードします。

## 使用上のヒント
- 元のトレードスタイルに対応するローソク足タイプを選ぶ（例：スイングトレーディングには6h/30m）。他の組み合わせも可能です。
- 好みのレスポンスに合わせてROC期間と平滑化方法を調整する。Jurik平滑化はソーススクリプトに最も近い動作を維持します。
- ポートは単純な成行出口を使用するため、ライブアカウントで運用する際は明示的なリスク管理（ストップロス、ポジションサイジング）の追加を検討してください。
