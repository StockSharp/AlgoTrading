# テスティネーターの戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この戦略は、MetaTrader エキスパート アドバイザ **Testinator v1.30a** の C# ポートです。ロングポジションのみをオープンし、バスケットとして管理します。それぞれの新規購入は、設定可能なテクニカル フィルターのセットが「true」を返し、価格が最小ピップ数だけ進んだ場合にのみ許可されます。終了ロジックは、別のフィルター マスクを使用して開始ロジックを反映します。元の EA もリスク管理のために日次の ATR 測定に依存していたため、ポートは主要な時間枠に加えて日次のローソク足をサブスクライブしています。

## 取引ロジック

### エントリフィルタマスク（パラメータ `BuySequence`）

マスクは下位 9 ビットを使用します。設定されたビットは、以前に完成したキャンドルの対応するテストを満たさなければなりません。

| ビット | 状態 |
| --- | --------- |
| 1 | EMA(12) は SMA(14) を上回っています。 |
| 2 | EMA(50) は、過去 3 つのローソク足の安値を下回ったままです。 |
| 4 | 前回の安値は、Bollinger バンドの下限 (20, 2) を下回っています。 |
| 8 | ADX(14) は -DI を上回っており、+DI は -DI よりも強力です。 |
| 16 | Stochastic (16, 4, 8) には %D より上の %K と 80 より上の %D があります。 |
| 32 | Williams %R(14) は -20 より大きいです。 |
| 64 | MACD(12, 26, 9) ラインは信号ラインの上にあります。 |
| 128 | Ichimoku は、センコウ スパン A がスパン B を上回り、テンカンがキジュンを上回り、前安値がスパン A を上回っていることを示しています。 |
| 256 | RSI (期間 `RsiEntryPeriod`) は `RsiEntryLevel` を超えており、前の値と比較して上昇しています。 |

### 終了フィルターマスク (パラメーター `CloseBuySequence`)

| ビット | 状態 |
| --- | --------- |
| 1 | SMA(14) は EMA(12) を上回っています。 |
| 2 | EMA(50) は、過去 3 つのローソク足の高値を上回っています。 |
| 4 | 前回の高値は上出口 Bollinger バンド (`BollingerCloseLength`、`BollingerCloseDeviation`) を上回っています。 |
| 8 | -DI が +DI を上回っています。 |
| 16 | Stochastic %D は 80 未満です。 |
| 32 | Williams %R(14) は -80 未満です。 |
| 64 | MACD 線が信号線の下にあります。 |
| 128 | Ichimoku センコウ スパン B がセンコウ スパン A を上回っています。 |
| 256 | RSI (期間 `RsiClosePeriod`) は `RsiCloseLevel` を下回っています。 |

バスケットは、すべてのアクティブなエントリービットが true を返し、購入数が `MaxBuys` 未満で、最後の約定価格が少なくとも `StepPips` 離れている場合にのみ延長されます。バスケットは、出口マスクが通過するとき、または保護レベルがトリガーされるたびに平らになります。

### セッション制御とリスク管理

* 取引は `TradeStartHour` と `TradeStartHour + TradeDurationHours - 1` (東ヨーロッパ時間) の間でのみ行われます。ウィンドウが閉じられており、バスケットに利益がある場合は、すべての買いがクローズされます。
* プロテクティブストップとテイクプロフィットディスタンスはpipsで表されます。値を `-1` に設定すると無効になり、`0` では ATR 乗数 (`StopRatio`、`TakeRatio`) が有効になります。
* トレーリングストップは、`StartTrailPips`、`TrailStepPips`、`StartTrailRatio`、および `TrailStepRatio` まで同じ ATR ロジックを使用します。
* この戦略では、D1 ローソク足の毎日の ATR(15) 値を計算して、動作を EA と同一に保ちます。

## パラメーター

* `TradeVolume` – すべての市場購入のロットサイズ（ボリューム）。
* `BuySequence` / `CloseBuySequence` – 個々のインジケーターフィルターを有効にするビットマスク。
* `MaxBuys` – バスケットとして処理されるオープン購入の最大数。
* `StepPips` – バスケットに追加する前の最小価格進行状況 (pips)。
* `TradeStartHour`、`TradeDurationHours` – 毎日の取引ウィンドウを定義します。
* `TakeProfitPips`、`StopLossPips` – 固定保護レベル（負の無効化、ゼロは ATR 比率に切り替わります）。
* `StartTrailPips`、`TrailStepPips` – トレーリングスタート距離とステップ（負の値は無効、ゼロは ATR 比率を使用します）。
* `TakeRatio`、`StopRatio`、`StartTrailRatio`、`TrailStepRatio` – 固定値がゼロの場合に使用される ATR 乗数。
* `RsiEntryLevel`、`RsiEntryPeriod` – エントリ マスクの RSI のしきい値と期間。
* `RsiCloseLevel`、`RsiClosePeriod` – 終了マスクの RSI のしきい値と期間。
* `BollingerCloseLength`、`BollingerCloseDeviation` – 出口 Bollinger バンドのパラメータ。
* `CandleType` – 稼働中のローソク足の時間枠（毎日のローソク足は、ATR に対して自動的にサブスクライブされます）。

## 注意事項

* ポートでは、元の EA のバスケット会計モデルが維持されます。注文はすべて買いであり、成行注文のみが使用されます。
* このロジックは、MetaTrader からの「bar[1]」チェックを模倣するために、以前のインジケーター値を意図的に保存します。
* この戦略では、EA の未使用の入力 (`TakeAsBasket`、`StopAsBasket` など) は、MQL ロジックに影響を与えなかったため無視します。
