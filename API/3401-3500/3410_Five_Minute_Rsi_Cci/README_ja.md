# FiveMinuteRsiCci 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

`FiveMinuteRsiCciStrategy` は、MetaTrader 4 Expert Advisor **5Mins Rsi Cci EA.mq4** の StockSharp ポートです。元のスクリプトは、RSI のしきい値クロスと平滑化/EMA 移動平均フィルター、および 2 つの CCI インジケーターの極性を組み合わせることにより、5 分間のローソク足を取引します。 C# バージョンは、データ サブスクリプション、インジケーター バインディング、リスク管理に StockSharp の高レベルの API を使用しながら、同じ意思決定ロジックを維持します。

## 取引ロジック

1. 設定されたローソク足タイプ (デフォルトでは 5 分の時間枠) をサブスクライブし、5 つのインジケーターをリアルタイムで更新します: RSI、始値の平滑化された MA、始値の EMA、さらに典型的な価格から計算された高速 CCI と低速 CCI。
2. 完了した各ローソク足は、オープンなポジションがなく、現在の買値/売値スプレッドが `MaxSpreadPoints` (価格単位に換算) を下回っている場合にのみ評価されます。
3. 長い信号には次のものが必要です。
   - EMA を超える平滑化された MA、
   - 以前のローソク足と現在のローソク足の間の `BullishRsiLevel` を通って上向きに交差する RSI、
   - 両方の CCI 値がゼロより大きい。
4. 短い信号には逆の条件が必要です（EMA 未満の平滑化された MA、RSI が `BearishRsiLevel` を下向きに交差、両方の CCI がゼロ未満）。
5. 注文量は、EA の動的なポジション サイズを再現します。`LotCoefficient × sqrt(Equity / EquityDivisor)` は商品の量ステップに四捨五入され、`VolumeMin`/`VolumeMax` によって制限されます。
6. 保護ロジックは `StartProtection` によって処理され、MetaTrader ポイントから絶対価格オフセットに変換されたストップロス、テイクプロフィット、トレーリングストップの距離が関連付けられます。

## パラメーター

| パラメータ | デフォルト | 説明 |
| --- | --- | --- |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` | インジケーターの更新とシグナル評価に使用されるタイムフレーム。 |
| `RsiPeriod` | `14` | RSI の計算で使用されるローソク足の数。 |
| `FastSmmaPeriod` | `2` | 始値に適用される高速平滑移動平均の期間。 |
| `SlowEmaPeriod` | `6` | 低速 EMA の期間がオープン価格に適用されます。 |
| `FastCciPeriod` | `34` | 通常価格 `(H+L+C)/3` から計算された高速 CCI の期間。 |
| `SlowCciPeriod` | `175` | 通常の価格から計算された低速の期間 CCI。 |
| `BullishRsiLevel` | `55` | ロングエントリーを準備するには、RSI のしきい値を上方に超える必要があります。 |
| `BearishRsiLevel` | `45` | ショートエントリーを準備するには、RSI のしきい値を下方に超える必要があります。 |
| `StopLossPoints` | `60` | MetaTrader ポイント単位のストップロス距離 (絶対価格に変換)。無効にするには、`0` に設定します。 |
| `TakeProfitPoints` | `0` | MetaTrader ポイント単位のテイクプロフィット距離。ゼロは、元の EA の動作を維持します (TP なし)。 |
| `TrailingStopPoints` | `20` | トレーリングストップ距離 (MetaTrader ポイント)。ゼロは末尾を無効にします。 |
| `LotCoefficient` | `0.01` | 動的位置サイジング式で使用される基本係数。 |
| `EquityDivisor` | `10` | 株式ベースのサイジングの平方根内の除数 (`sqrt(Equity / EquityDivisor)`)。 |
| `MaxSpreadPoints` | `18` | 最大許容スプレッド (MetaTrader ポイント単位)。スプレッドが狭くなるまで注文はスキップされます。 |

## 注意事項

- 拡散フィルタはレベル 1 データに依存します。最良の買値/売値が利用できない場合、戦略は新しいポジションをオープンする前に待機します。
- ポイントから価格への変換は、`PriceStep` と商品精度 (5/3 小数商品はステップに 10 を乗算) によって自動的にスケーリングされ、MetaTrader の `Point` 値を反映します。
- ストップとトレーリングは、StockSharp の市場エグジットを備えた組み込み保護エンジンを通じて管理され、トレーリングストップ更新のための EA の成行注文の使用と一致します。
