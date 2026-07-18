# CorrTime戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

CorrTime戦略は、同名のMetaTraderエキスパートアドバイザーを再現する単一シンボルのシステムです。終値とその時系列順序の相関を分析し、モメンタムの加速または反転を検出します。アルゴリズムは確定済みローソク足で動作し、3つの確認層を組み合わせます。

1. **ボラティリティフィルター:** ボリンジャーバンド幅は、許容される活動量の設定範囲内にある必要があります。これにより、横ばい局面と過度に荒い局面を避けます。
2. **トレンド強度フィルター:** 相関シグナル評価前に、Average Directional Index（ADX）がしきい値を上回り続ける必要があります。
3. **相関トリガー:** Pearson、Spearman、Kendall、Fechnerの相関推定器が、価格が時間とどれだけ密接に進むかを測定します。係数の急変が取引判断を生成します。

元のロボットはH1時間軸のEURUSD向けに設計されていましたが、StockSharp版はすべてのパラメーターを設定可能にしています。既定設定は元に忠実です（1時間足、Fechner相関、逆張りモード）。

## 取引ワークフロー

1. 選択した`CandleType`を購読し、確定バーを待ちます。
2. 新しいローソク足でボリンジャーバンドとADX値を更新します。
3. 次の場合、バーを拒否します。
   - pipsへ変換したボリンジャースプレッドが`[BollingerSpreadMin, BollingerSpreadMax]`の外にある。
   - ADXが`AdxLevel`を下回る。
   - ローソク足が`[EntryHour, EntryHour + OpenHours]`取引時間窓の外で始まる（深夜跨ぎ対応）。
4. 終値のローリング履歴を構築し、`CorrelationRangeTrend`と`CorrelationRangeReverse`のルックバックで相関係数を計算します。コードは直近3つの相関値を再計算し、元のincludeファイルがバッファーで行っていたのと同様に、制限の実際のクロスを検出します。
5. トレンドフォロートリガー（`TradeMode`が*TrendFollow*または*Both*の場合）:
   - **ロング:** 相関が`CorrLimitTrendBuy`を下回っており、前バーでも下回ったまま、最新バーでしきい値を上抜ける。
   - **ショート:** 相関が`-CorrLimitTrendSell`を上回っており、前バーでも上回ったまま、最新バーで`-CorrLimitTrendSell`を下抜ける。
6. 反転トリガー（`TradeMode`が*Reverse*または*Both*の場合）:
   - **ロング:** 相関が`-CorrLimitReverseBuy`を下回っており、前バーでも下回ったまま、最新バーで`-CorrLimitReverseBuy`を上回る。
   - **ショート:** 相関が`CorrLimitReverseSell`を上回っており、前バーでも上回ったまま、最新バーで`CorrLimitReverseSell`を下回る。
7. 両方向が同時に発火した場合、MetaTraderの動作を反映してシグナルは互いに打ち消します。
8. `CloseTradeOnOppositeSignal`が有効な場合、新しいポジションを開く前に反対ポジションを即座に閉じます。
9. エントリーは`Volume`プロパティでサイズ設定され、`MaxOpenOrders`を尊重するため、ネットエクスポージャーはどちらの方向でも`Volume * MaxOpenOrders`を超えません。
10. リスクは`StartProtection`で制御されます。ストップロスとテイクプロフィットはpipベース距離を使い、トレーリングフラグが有効な場合は同じストップ距離を再利用します。

## パラメーター

| パラメーター | 説明 |
|-----------|-------------|
| `CandleType` | ローソク足生成とすべての指標供給に使う時間軸。 |
| `CloseTradeOnOppositeSignal` | 次のシグナルが反対方向を示すとき、オープンポジションを閉じます。 |
| `EntryHour`, `OpenHours` | 日次取引時間窓を定義します。`OpenHours = 0`は窓を1時間だけ開いたままにします。 |
| `BollingerPeriod`, `BollingerDeviation` | 終値に適用する標準ボリンジャーバンド設定。 |
| `BollingerSpreadMin`, `BollingerSpreadMax` | ボリンジャーチャンネルに必要な最小/最大幅（pips）。 |
| `AdxPeriod`, `AdxLevel` | Average Directional Index設定と必要な最小トレンド強度。 |
| `TradeMode` | トレンドフォロー、反転、または組み合わせ評価から選択します。 |
| `CorrelationRangeTrend`, `CorrelationRangeReverse` | 相関計算のルックバック長。 |
| `CorrelationType` | Pearson、Spearman、Kendall、Fechnerの相関式を選択します。 |
| `CorrLimitTrendBuy`, `CorrLimitTrendSell` | 有効なトレンドフォローブレイクアウトを定義するしきい値。 |
| `CorrLimitReverseBuy`, `CorrLimitReverseSell` | 有効な反転ブレイクアウトを定義するしきい値。 |
| `TakeProfitPips`, `StopLossPips`, `TrailingStopPips` | pipsで表し、銘柄pipサイズで価格単位へ変換するリスクパラメーター。 |
| `MaxOpenOrders` | 集約エントリー数の上限（片側上限は`Volume * MaxOpenOrders`）。 |

## 実用メモ

- pipサイズは銘柄の小数桁から推定されます（小数5桁または3桁は10倍乗数に対応）。MetaTraderのポイント処理を模倣します。非FX資産ではしきい値を調整してください。
- 相関バッファーはクロスを評価するために少なくとも`lookback + 2`本の確定ローソク足を必要とします。ウォームアップ中、戦略は待機します。
- すべてのロジックが確定ローソク足で実行されるため、戦略はバー内ノイズに強く、`iTime`と`iClose`スナップショットに依存した元の動作を反映します。
- 複数インスタンスを展開する場合は、ポートフォリオレベルのリスク制御と組み合わせてください。元のロボットもシンボル全体の合計注文数を制限していました。
