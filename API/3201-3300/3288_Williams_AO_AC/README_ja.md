# Williams AO + AC戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**Williams AO + AC戦略**は、MetaTrader 4 エキスパート "Williams_AOAC" を StockSharp の高レベル戦略 API へ変換します。このアプローチは、Bill Williams の複数のツールを組み合わせ、時間足チャート (デフォルト時間枠) で momentum の急増を見つけます。

1. **Bollinger Bandフィルター** - バンド幅が設定可能なポイント範囲内にある場合にのみ取引し、横ばい市場と過度なボラティリティの両方を避けやすくします。
2. **Relative Strength Index確認** - RSI はロングでは強気しきい値を上回り、ショートでは弱気しきい値を下回る必要があります。
3. **Awesome Oscillatorのゼロラインクロス** - オシレーターは取引方向にゼロ軸をクロスする必要があり、momentum の変化を示します。
4. **Accelerator Oscillatorの加速** - 直近 3 つの Accelerator 値がゼロの同じ側にあり、最新バーがその動きを伸ばしている必要があり、加速を確認します。
5. **取引セッションフィルター** - エントリーは、日の時刻で表される設定可能な時間窓内でのみ許可されます。

確定した各ローソク足で、戦略は `Bind` パイプラインから渡されるインジケーター値を処理します。すべてのフィルターがそろうと、必要に応じて反対ポジションを閉じ、要求されたロットサイズで新しい成行注文を開きます。stop-loss と take-profit は価格ポイントの距離で適用され、任意の trailing stop は取引が利益化した後に保護ストップを引き締めます。

## エントリールール
### ロング条件
1. Bollinger スプレッド (上側バンドから下側バンドを引き、ポイントに変換したもの) が **BollingerSpreadLower** と **BollingerSpreadUpper** の間にあります。
2. RSI 読み取り値が **RsiBuyThreshold** より厳密に大きいです。
3. Awesome Oscillator が現在バーでマイナスからプラスへクロスします。
4. 直近 3 本のローソク足の Accelerator Oscillator 値がすべてプラスで、最新値が前の値より高く、強気 momentum の増加を示します。
5. 現在バーの始値時刻が、**EntryHour** に始まり **TradingWindowHours** 時間続く取引窓内にあります (必要なら日付をまたぎます)。
6. 戦略はまだロングポジションを持っていません (フラットまたはショートの可能性があります)。

ロジックが満たされると、戦略はすべてのショートエクスポージャーを閉じ、**TradeVolume** でロング成行注文を開き、設定された stop-loss / take-profit 距離を適用します。取引が少なくとも **TrailingStopPoints** だけ有利に動いた後、trailing stop の追跡が始まります。

### ショート条件
1. Bollinger スプレッドが許可範囲内にあります。
2. RSI 読み取り値が **RsiSellThreshold** より厳密に小さいです。
3. Awesome Oscillator が現在バーでプラスからマイナスへクロスします。
4. 直近 3 本のローソク足の Accelerator Oscillator 値がすべてマイナスで、最新値が前の値より低く、弱気圧力の増加を示します。
5. ローソク足の始値時刻が取引セッション窓内にあります。
6. 戦略はまだショートポジションを持っていません (フラットまたはロングの可能性があります)。

発動すると、モジュールはロングエクスポージャーを閉じ、**TradeVolume** でショート成行注文に入り、保護注文を再割り当てします。

## エグジット管理
* **Take-profit** - **TakeProfitPoints** がゼロより大きい場合、エントリー価格からその数の価格ポイントだけ離れた利益目標を各新規ポジションに付与します。
* **Stop-loss** - **StopLossPoints** がゼロより大きい場合、エントリー価格を基準に固定ストップを適用します。
* **Trailing stop** - **TrailingStopPoints** がゼロより大きい場合、利益が trailing 距離を超えると stop-loss を市場へ近づけます。ロング取引ではストップを `Close - TrailingStopPoints * pip` へ引き上げ、ショートでは `Close + TrailingStopPoints * pip` へ引き下げます。trailing は一方向です。ストップは戻りません。
* ユーザーによる手動ポジション変更は尊重されます。trailing ロジックは、エンジンが報告する現在の集計ポジションに反応します。

## パラメーター
| 名前 | 説明 | デフォルト |
|------|------|------------|
| `CandleType` | 計算に使う主要ローソク足系列。 | 1 時間足 |
| `BollingerPeriod` | Bollinger Bands のルックバック期間。 | 20 |
| `BollingerDeviation` | 標準偏差乗数。 | 2.0 |
| `BollingerSpreadLower` | 取引を有効にするために必要な最小バンド幅 (ポイント)。 | 40 |
| `BollingerSpreadUpper` | 取引に許可される最大バンド幅 (ポイント)。 | 210 |
| `AoFastPeriod` | Awesome Oscillator の短期期間。 | 11 |
| `AoSlowPeriod` | Awesome Oscillator の長期期間。 | 40 |
| `RsiPeriod` | RSI 計算長。 | 20 |
| `RsiBuyThreshold` | ロング取引の最小 RSI 値。 | 46 |
| `RsiSellThreshold` | ショート取引の最大 RSI 値。 | 40 |
| `EntryHour` | 取引窓が始まる時刻 (0-23)。 | 0 |
| `TradingWindowHours` | 許可される取引窓の長さ (時間)。`0` は開始時刻のみを保持します。 | 20 |
| `TradeVolume` | 各新規ポジションのロットサイズ。 | 0.01 |
| `StopLossPoints` | 価格ポイント単位の stop-loss 距離。 | 60 |
| `TakeProfitPoints` | 価格ポイント単位の take-profit 距離。 | 90 |
| `TrailingStopPoints` | 価格ポイント単位の trailing stop 距離。 | 30 |

## 追加の注意事項
* Accelerator Oscillator 値は、現在の AO 読み取り値から Awesome Oscillator の 5 期間単純移動平均を引くことで内部的に導出され、元エキスパートが使用した MetaTrader 実装と一致します。
* バンドスプレッド計算は商品の `PriceStep` に依存します。利用できない場合、戦略は生の価格差にフォールバックします。
* `EntryHour + TradingWindowHours` が 23 を超える場合、取引セッション窓は日付をまたぎ、MetaTrader の時間フィルターを再現します。
* 戦略は新しいポジションを開く前に反対エクスポージャーを自動的に閉じ、元の MQL4 コードの単一注文制限を再現します。
