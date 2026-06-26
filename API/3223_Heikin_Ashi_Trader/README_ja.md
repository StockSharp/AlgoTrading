# Heikin Ashi Trader戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTrader 4のエキスパート「Heikin Ashi Trader」をStockSharpに移植します。元のロボットのマルチインジケーター確認ロジックを維持し、すべての決定が完成したバーのみに基づくよう高レベルのローソク足サブスクリプションAPIで実装します。

## 詳細
- **インジケーター**:
  - 作業時間軸から計算されたHeikin-Ashiローソク足。
  - 典型的なローソク足価格（`(high + low + close) / 3`）を使用した2つの線形加重移動平均（LWMA）。
  - ストキャスティクスオシレーター（`%K/%D/Smooth`期間はユーザー設定可能）。
  - Momentum（ニュートラル100レベルからの距離）。
  - 移動平均収束拡散（MACD）。
- **エントリー条件**:
  - **ロング**: 最新のHeikin-Ashiローソク足が強気、直近3つのストキャスティクス値のうち少なくとも1つが買われすぎレベルを超え、ファストLWMAがスローLWMAを上回り、100からのモメンタム距離が買いしきい値を超え、MACDラインがシグナルを上回る必要がある。
  - **ショート**: 反転条件 — 弱気Heikin-Ashiローソク足、売られすぎレベル以下のストキャスティクス、スローLWMAを下回るファストLWMA、売りしきい値を超えるモメンタム距離、シグナルを下回るMACDライン。
  - 新しい取引前に反対の露出をオプションでフラット化（`CloseOppositePositions`）。
- **ポジション管理**:
  - ピップ単位の固定ストップロスとテイクプロフィット（銘柄の価格ステップから導出）。
  - 取引が`TrailingStopPips`だけ前進したらクローズに追従するオプショナルトレーリングストップ。
  - 価格がポジションに有利な方向に`BreakEvenTriggerPips`進んだ後、ストップを`Entry ± BreakEvenOffsetPips`に移動するブレークイーブンロジック。
  - 次のローソク足ですべてをフラット化する手動キルスイッチ（`ForceExit`）。
- **MT4バージョンとの違い**:
  - 元のEAはモメンタムを上位時間軸で評価していた。このポートはStockSharpの高レベルAPI内に留まるために、同じインジケーター期間を維持しながら主要ローソク足ストリームから読み取る。パラメーターにより元の感度を再現するためにしきい値を調整できる。
  - MT4コードの金額ベースのストップルールは含まれていない。リスクは価格ベースのストップとブレークイーブンモジュールで管理される。

## パラメーター
| 名前 | 説明 |
| --- | --- |
| `CandleType` | すべてのインジケーターとトレード決定に使用される時間軸（またはその他のローソク足タイプ）。 |
| `FastMaPeriod`, `SlowMaPeriod` | ファストとスローの線形加重移動平均（典型的価格）の期間。 |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | ストキャスティクスオシレーターの`%K/%D`長と平滑化ファクター。 |
| `StochasticOverbought`, `StochasticOversold` | 直近3つの完成値の間にクロスする必要があるストキャスティクスしきい値。 |
| `MomentumPeriod` | Momentumインジケーターの長さ。 |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | ロング/ショート取引に必要な100ラインからの最小絶対距離。 |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD設定。 |
| `CloseOppositePositions` | 新しい取引に入る前に反対側を閉じる。 |
| `MaxPositions` | 方向ごとの最大純露出（`0` = 無制限）。 |
| `TradeVolume` | 各新規注文のボリューム；戦略の`Volume`にも割り当てられる。 |
| `UseStopLoss`, `StopLossPips` | ピップ単位の保護ストップを有効化しサイズを設定する。 |
| `UseTakeProfit`, `TakeProfitPips` | ピップ単位のテイクプロフィットを有効化しサイズを設定する。 |
| `UseTrailingStop`, `TrailingStopPips` | トレーリングストップロジックを有効化し距離を定義する。 |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | ブレークイーブン起動距離とロックされたオフセット。 |
| `ForceExit` | `true`のとき、次の処理ローソク足ですべてのポジションが閉じられる。 |

## 実装上の注意
- 戦略は`SubscribeCandles().BindEx(...)`を通じてローソク足をサブスクライブするので、インジケーターは完成した値を受け取り、コードは`GetValue()`を直接呼び出さない。
- ピップ変換はインストゥルメントの`PriceStep`を使用；市場が小数ピップを参照する場合、銘柄ステップを適切に設定する。
- トレーリングとブレークイーブンの更新はストップを有利な方向にのみ移動する。リセットロジックは取引が閉じられるたびにキャッシュされたストップ/ターゲット値をクリアし、新しいポジションが新鮮なリスク設定で開始するようにする。
