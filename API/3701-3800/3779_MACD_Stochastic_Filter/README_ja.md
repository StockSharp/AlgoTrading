# MACD + Stochastic トレンド フィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、フォルダ `MQL/7604` から MetaTrader エキスパート アドバイザーの動作を再作成します。元のスクリプトは、緑と赤のバッファーを生成するカスタム オシレーターに依存していました。実際には、数値 `(15, 3, 3)` は古典的な確率発振器と一致するため、StockSharp ポートは信号確認に組み込みの `Stochastic` インジケーターを使用し、MACD と EMA トレンド フィルターが方向を管理します。

この戦略はロングとショートの両方で取引されます。取引方向の確率的クロスオーバーを待ち、MACD ヒストグラムがゼロから十分な距離でシグナルラインを横切ることを要求し、EMA の傾きがエントリーと一致することを要求します。リスク管理は、MQL バージョンを反映しています。固定のストップロス、テイクプロフィット、および取引が利益に転じるとすぐに保護レベルを強化するポイントベースのトレーリング ストップです。

## インジケーター

- **MovingAverageConvergenceDivergenceSignal** パラメータ `fast = 12`、`slow = 26`、`signal = 9`。 MACD ヒストグラムは、長いセットアップの場合はゼロ未満、短いセットアップの場合はゼロより上に留まりながら、信号線を横切る必要があります。追加のしきい値 (`MacdOpenLevel`、`MacdCloseLevel`) により、ゼロラインからの絶対距離が最小になります。
- **Stochastic** オシレーターと `(Length = 15, KPeriod = 3, DPeriod = 3)`。 %K ラインは「緑」バッファの役割を果たし、ロングトレードの場合は %D より上でなければなりません (ショートトレードの場合は下にあります)。同じクロスオーバーがポジションを終了するために使用されます。
- 期間 `26` の **ExponentialMovingAverage**。 EMA は方向性フィルターを提供します。ロングトレードの場合、現在の EMA 値は前のバーの EMA を上回る必要があり、ショートトレードの場合はその逆です。

## エントリーロジック

1. **長いセットアップ**
   - Stochastic 現在閉じているローソク足では %K > %D。
   - 現在の足の MACD ヒストグラム < 0 および > シグナル ライン。
   - MACD ヒストグラム < 前のバーのシグナルライン (つまり、現在強気のクロスオーバー)。
   - `|MACD| > MacdOpenLevel * price_step`。
   - EMA が上昇しています (現在の EMA > 以前の EMA)。
2. **簡単なセットアップ**
   - 現在のローソク足では Stochastic %K < %D。
   - MACD ヒストグラム > 0 および < シグナルラインが現在のバーにあります。
   - MACD ヒストグラム > 前のバーのシグナルライン (弱気クロスオーバー)。
   - `MACD > MacdOpenLevel * price_step`。
   - EMA が低下しています (現在の EMA < 前回の EMA)。

アカウントがすでにポジションを保持している場合、開いている取引が終了するまで新しい注文は生成されません。

## 終了ロジック

ポジションがオープンしている間、戦略は次のことを継続的に適用します。

- **インジケーターの出口**
  - `%K < %D`、MACD > 0、MACD < シグナル、前の MACD がシグナルを上回り、絶対ヒストグラムが `MacdCloseLevel * price_step` を超えた場合、ロング ポジションは閉じます。
  - ショート ポジションは、`%K > %D`、MACD < 0、MACD > シグナル、前の MACD がシグナルを下回ったとき、および `|MACD| > MacdCloseLevel * price_step` のときにクローズされます。
- **ストップロス**: `StopLossPoints` によって設定され、商品の `PriceStep` を介して価格単位に変換されます。
- **利益確定**: `TakeProfitPoints` に `PriceStep` を掛けます。
- **トレーリング ストップ**: 利益が `TrailingStopPoints * PriceStep` を超えると、ストップ レベルが (ロングの場合) 上昇または下降 (ショートの場合) されるため、取引では常に少なくともその額の利益が固定されます。

## パラメーター

| 名前 | 説明 | デフォルト |
| --- | --- | --- |
| `TradeVolume` | ロット単位での注文サイズ | `0.1` |
| `TakeProfitPoints` | テイクプロフィット距離（ポイント単位） | `10` |
| `StopLossPoints` | ポイント単位のストップロス距離 | `50` |
| `TrailingStopPoints` | トレーリングストップ距離（ポイント単位） | `5` |
| `MacdOpenLevel` | エントリの最小絶対値 MACD | `3` |
| `MacdCloseLevel` | 出口の最小絶対値 MACD | `2` |
| `MacdFastPeriod` | MACD 内の高速な EMA の長さ | `12` |
| `MacdSlowPeriod` | MACD 内で EMA の長さが遅い | `26` |
| `MacdSignalPeriod` | MACD 信号の長さ EMA | `9` |
| `EmaPeriod` | トレンド フィルターの EMA 期間 | `26` |
| `StochasticLength` | Stochastic ルックバック ウィンドウ | `15` |
| `StochasticKPeriod` | %K スムージング | `3` |
| `StochasticDPeriod` | %D スムージング | `3` |
| `CandleType` | 計算に使用される時間枠 | `15m` |

## 注意事項

- すべての計算では、元の EA の `start()` ループと一致する完成したキャンドルのみを使用します。
- 機器が提供する `PriceStep` は 1 つの点を定義します。セキュリティがステップを公開しない場合、戦略は `1` に戻ります。
- このコードは純粋に StockSharp の高レベルの API に依存しています。インジケーターは `SubscribeCandles().BindEx(...)` を介してバインドされ、手動履歴バッファーは作成されず、注文は MQL バージョンと同様に `BuyMarket`/`SellMarket` を使用します。
