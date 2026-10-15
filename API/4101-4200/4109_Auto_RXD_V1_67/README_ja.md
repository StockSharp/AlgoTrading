# Auto RXD v1.67 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Auto RXD v1.67 は、同じ名前の MetaTrader エキスパート アドバイザをエミュレートするルールベースの戦略です。このアプローチでは、強気シグナルか弱気シグナルのどちらを探すかを決定するスーパーバイザーと、各方向の専用パーセプトロンという 3 つの線形パーセプトロンを使用します。すべてのパーセプトロンは、ローソク足の終値とロビー・ルアンの「加重価格」（高値 + 安値 + 2 × 終値）入力から計算された線形加重移動平均 (LWMA) に基づいて動作します。 StockSharp ポートは完了したローソク足に対してのみ実行され、高レベルの `BindEx` データ フローを使用してインジケーターの計算を取引ループと同期させます。

## 市場データと指標
- **ローソク足** – デフォルトの時間枠は 30 分のローソク足です。時間枠は、`CandleType` パラメータを通じて変更できます。
- **平均トゥルーレンジ (ATR)** – `UseAtrTargets` が有効な場合、適応テイクプロフィット距離とストップロス距離の両方を提供します。 ATR 期間は `AtrPeriod` によって制御されます。
- **相対強度指数 (RSI)** – `UseRsiFilter` が true の場合、ニュートラル 50 レベルを超えるロング取引と 50 未満のショート取引を強制するオプションのフィルター。
- **コモディティ チャネル インデックス (CCI)** – `UseCciFilter` がアクティブな場合、ロングの場合は +100 を超え、ショートの場合は -100 未満の測定値を必要とするオプションのトレンド フィルター。
- **移動平均収束発散 (MACD)** – オプションの運動量の確認。ロングエントリーにはシグナルラインの上にある MACD ラインが必要ですが、`UseMacdFilter` が true の場合、ショートにはシグナルラインの下にある MACD ラインが必要です。
- **平均方向インデックス (ADX)** – ADX が設定されたしきい値を上回っていること、および `UseAdxFilter` が有効な場合に +DI と -DI が目的の方向と一致していることをチェックするオプションの強度フィルター。

## 取引ロジック
1. **パーセプトロン データの準備** – ローソクごとに、ストラテジーは最新の終値と加重価格でバッファを更新します。バッファーは LWMA スナップショットをフィードし、ショート、ロング、スーパーバイザー パーセプトロンに対して構成された `Step` 値で区切られた 4 つのラグ特徴を生成します。
2. **スーパーバイザーの決定** – スーパーバイザーのパーセプトロンは、重みパラメーター `SupervisorX1…X4` および `SupervisorThreshold` を使用してラグデルタを評価します。プラスのスコアは長いパーセプトロンのロックを解除します。マイナスのスコアは短いパーセプトロンのロックを解除します。スーパーバイザーのスコアがゼロであるか、使用できない (十分なデータがない) 場合、ローソク足はスキップされます。
3. **方向スペシャリスト** – 一致するパーセプトロン (ロングまたはショート) は、同じ LWMA 特徴セットと方向固有の重み (`LongX*` または `ShortX*`) を使用して独自のスコアを検証します。正の値を指定すると、次の検証段階がトリガーされます。
4. **インジケーター フィルター** – `UseIndicatorFilters` が false の場合、戦略はパーセプトロン信号のみに基づいて取引されます。 true の場合、有効な各フィルター (RSI、CCI、MACD、ADX) は、提案された方向に一致する必要があります。インジケーター データが欠落しているか、条件に失敗すると、シグナルがキャンセルされます。
5. **注文執行** – この戦略は、アクティブな注文がないことを保証し、逆のエクスポージャーをフラットにし、`OrderVolume` のサイズの成行注文を使用してエントリーします。エントリ価格は、利用可能な場合はデフォルトで最良相場を設定し、それ以外の場合はローソク足で終了します。

## リスク管理
- **保護注文** – エントリーを入力すると、ストラテジーはすぐに `CalculateProtectiveDistances` を介してテイクプロフィットとストップロスの距離を計算します。 `UseAtrTargets` が true の場合、距離は設定された乗数 (`AtrTakeProfitFactor`、`AtrStopLossFactor`) と元の MQL ポイントベースの TP/SL の大きさによって ATR スケールされます。 ATR ターゲティングが無効になっている場合、固定小数点距離は価格ステップに変換されます。
- **注文管理** – ヘルパー `SetProtectiveOrders` は、生の距離を価格ステップ数に変換し、エントリー価格に対するストップロス注文とテイクプロフィット注文を登録します。この戦略では、新しい取引を送信する前に `HasActiveOrders()` をチェックすることで、重複注文を回避します。
- **保護の開始** – `StartProtection()` は `OnStarted` で 1 回呼び出され、位置がゼロ以外になるたびにフレームワークの組み込み保護処理が有効になります。

## パラメーター
StockSharp 実装は、最適化と UI の明瞭さのためにグループ化された完全な MQL パラメータ セットを公開します。主要なパラメータは次のとおりです。

### 取引
- `OrderVolume` – 新しいポジションのロットサイズ。
- `CandleType` – バインディングに使用されるキャンドルのデータ型。

### リスク
- `UseAtrTargets` – ATR ベースと固定小数点の保護距離を切り替えます。
- `AtrPeriod`、`AtrTakeProfitFactor`、`AtrStopLossFactor` – アダプティブ ターゲットの ATR 構成。
- `LongTakeProfitPoints`、`LongStopLossPoints`、`ShortTakeProfitPoints`、`ShortStopLossPoints` – ATR と固定モードの両方で再利用されるポイントベースの TP/SL 参照。

### インジケーターフィルター
- `UseIndicatorFilters` – すべてのフィルターのマスター スイッチ。
- `UseAdxFilter`、`AdxPeriod`、`AdxThreshold` – ADX の確認設定。
- `UseMacdFilter`、`MacdFast`、`MacdSlow`、`MacdSignal` – MACD の確認設定。
- `UseRsiFilter`、`RsiPeriod` – RSI の確認設定。
- `UseCciFilter`、`CciPeriod` – CCI の確認設定。

### パーセプトロンスペシャリスト
- `ShortMaPeriod`、`ShortStep`、`ShortX1…ShortX4`、`ShortThreshold` – 短いパーセプトロン構成。
- `LongMaPeriod`、`LongStep`、`LongX1…LongX4`、`LongThreshold` – 長いパーセプトロン構成。
- `SupervisorMaPeriod`、`SupervisorStep`、`SupervisorX1…SupervisorX4`、`SupervisorThreshold` – スーパーバイザーのパーセプトロン構成。

すべての数値パラメータは MQL のデフォルトを反映しており、最適化キャンペーンのために `StrategyParam` システムを通じて構成を公開しながら、元のエキスパート アドバイザとこの StockSharp ポートの間で同様の動作が可能になります。
