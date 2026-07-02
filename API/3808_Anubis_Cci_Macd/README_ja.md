# アヌビス CCI MACD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader 4 エキスパートアドバイザー「アヌビス」を StockSharp の高レベル API に変換します。
- 4 時間の商品チャネル インデックス (CCI) フィルターと 15 分の MACD クロスオーバーを使用します。
- 適応的なポジションサイジング、ストップロス、損益分岐点保護、ATR 主導のエグジット、標準偏差ベースのテイクプロフィットを適用します。

## 戦略ロジック
1. **データ**
   - 主な時間枠: 15 分のローソク足 (`SignalCandleType`)、MACD および ATR の計算に使用されます。
   - より長い時間枠: 4 時間足ローソク足 (`TrendCandleType`)、CCI のフィルタリングと標準偏差の測定に使用されます。
2. **指標**
   - 4H シリーズでは期間を設定できる `CommodityChannelIndex`。
   - 4H の `StandardDeviation` (長さ 30) はテイクプロフィットディスタンスを推定するためにクローズされます。
   - 15M キャンドルで `MovingAverageConvergenceDivergenceSignal` (高速/低速/信号設定可能)。
   - ボラティリティベースのエグジット用の 15M ローソク足の `AverageTrueRange` (長さ 12)。
3. **エントリー**
   - **ショート**: 4 時間足 CCI が `CciThreshold` を上回っている場合、前の 2 つの MACD の値は弱気のクロスオーバー (MACD がそのシグナルを下回る) を示し、MACD はプラスであり、未決済のロングはなく、最後のショートエントリー以降、価格は少なくとも `PriceFilterPoints` 移動しています。
   - **ロング**: CCI が `-CciThreshold` の下にあり、MACD が負で上に交差する対称条件、オープンショートがなく、最小距離フィルターが満たされています。
4. **リスク管理**
   - 基本ボリュームは `VolumeValue` で定義され、口座資本 (14,000 を超えると 2 倍、22,000 を超えると 3.2 倍) と負け取引後の `LossFactor` によってスケールされます。
   - 方向ごとの最大同時取引数は、`MaxLongTrades` と `MaxShortTrades` によって制限されます。
   - ハードストップロスは実質的に平均エントリー価格から `StopLossPoints * PriceStep` に設定されます。
   - 価格が `BreakevenPoints` 進むと損益分岐点が有効になり、価格がエントリーに戻るとすぐにポジションを閉じます。
5. **出口**
   - 標準偏差のテイクプロフィットは、価格が `StdDevMultiplier * StdDev` に有利に動くとポジションを閉じます。
   - 積極的な出口は、前のローソク足の範囲が `CloseAtrMultiplier * ATR` を超えたときにトリガーされます。
   - MACD の減速出口には、十分な利益 (`ProfitThresholdPoints`) と、MACD の勾配での反転 (方向に応じて、前の MACD が 2 バーより小さいかそれより大きい) の両方が必要です。
   - 価格がストップロス距離を突破するか、損益分岐点アクティブ化後にエントリーに戻る場合、プロテクティブストップは取引を終了します。

## パラメーター
| 名前 | 説明 |
| ---- | ----------- |
| `VolumeValue` | 基本注文量。 |
| `CciThreshold` | 4H CCI フィルターの絶対しきい値。 |
| `CciPeriod` | 4H CCI インジケーターの期間。 |
| `StopLossPoints` | ポイント単位のストップロス距離。 |
| `BreakevenPoints` | 損益分岐点を達成するために必要なポイントでの利益。 |
| `MacdFastPeriod` | MACD の高速 EMA 期間。 |
| `MacdSlowPeriod` | MACDのEMA期間が遅いです。 |
| `MacdSignalPeriod` | MACD の EMA 期間を信号で知らせます。 |
| `LossFactor` | 負けた取引後に適用される出来高乗数。 |
| `MaxShortTrades` | 同時ショートエントリーの最大数。 |
| `MaxLongTrades` | 同時ロングエントリの最大数。 |
| `CloseAtrMultiplier` | 早期終了の乗数は ATR です。 |
| `ProfitThresholdPoints` | MACD が終了する前に追加の利益バッファー (ポイント)。 |
| `StdDevMultiplier` | テイクプロフィットの標準偏差乗数。 |
| `PriceFilterPoints` | 連続するエントリー間の最小の価格変動。 |
| `SignalCandleType` | MACD と ATR の主な時間枠。 |
| `TrendCandleType` | CCI の時間枠と標準偏差が高くなります。 |

## 注意事項
- この戦略は、有効な `Security.PriceStep` メタデータに依存して、ポイントベースのパラメータを価格距離に変換します。
- 保護ロジックは、保留中のストップ/リミット注文の代わりに明示的なチェックによって実装され、仮想ストップによる元の EA の動作を反映します。
- Python のバージョンは、タスクの手順ごとに意図的に省略されています。
