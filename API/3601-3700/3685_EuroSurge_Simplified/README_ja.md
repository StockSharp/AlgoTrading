# ユーロサージの簡素化された戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader 4 エキスパート アドバイザー **「EuroSurge Simplified」** を StockSharp の高レベル API に変換します。
- 完成したローソク足を取引し、古典的なインジケーターのコレクション (MA、RSI、MACD、Bollinger バンド、Stochastic) を評価してエントリーを見つけます。
- 取引間に設定可能なクールダウン期間を強制し、価格ステップで表現されるテイクプロフィット/ストップロスレベルを付加します。
- 複数のポジションサイジングモード（固定ボリューム、バランスパーセンテージ、資本パーセンテージ）をサポートします。

## 信号
1. **移動平均トレンド** (オプション): 高速の 20 期間 SMA は、低速の構成可能な SMA より上 (ロング) または下 (ショート) である必要があります。
2. **RSI フィルター** (オプション): RSI は買いを許可するには長期しきい値を下回り、売りを許可するには短期しきい値を上回る必要があります。
3. **MACD 確認** (オプション): MACD ラインは信号ラインより大きい (長い) か、信号ラインより小さい (短い) 必要があります。
4. **Bollinger バンド フィルター** (オプション): 価格はロングの場合は下位バンド、ショートの場合は上位バンドを突破する必要があります。
5. **Stochastic フィルター** (オプション): %K と %D は両方とも、ロングの場合は 50 未満、ショートの場合は 50 を超える必要があります。

ストラテジーが成行注文を送信する前に、有効になっているすべてのフィルターが一致する必要があります。逆のエクスポージャーは、オープン取引を置き換える MetaTrader ロジックを反映して、新しいポジションをオープンする前にフラット化されます。

## リスク管理
- ストップロスとテイクプロフィットの距離は価格ステップ（MetaTrader「ポイント」）で定義されます。
- この戦略は、ポジションをオープンした直後に、`SetStopLoss` と `SetTakeProfit` で保護注文を自動的に登録します。
- 取引は、最後に約定した注文から設定された分単位の間隔が経過するまでブロックされます。

## ポジションサイズ
- **FixedSize**: 設定された `FixedVolume` で取引されます。
- **BalancePercent**: ポートフォリオの開始残高の一部を割り当て、最新の終値で割って出来高を概算します。
- **EquityPercent**: 同じように動作しますが、現在のポートフォリオの資本に依存します。
- ボリュームはセキュリティ ボリューム ステップにスナップされ、エクスチェンジの最小/最大制限の間にクランプされます。

## パラメーター
| 名前 | 説明 |
| ---- | ----------- |
| `TradeSizeType` | ポジションサイジングモード（固定、バランス％、エクイティ％）。
| `FixedVolume` | `TradeSizeType = FixedSize` のときに使用されるボリューム。
| `TradeSizePercent` | パーセントベースのサイジングに適用されるパーセント。
| `TakeProfitPoints` / `StopLossPoints` | 価格段階における保護距離。
| `MinTradeIntervalMinutes` | 取引間のクールダウン。
| `MaPeriod` | 低速の SMA の長さ（高速の SMA は、EA に合わせて 20 に固定されています）。
| `RsiPeriod`, `RsiBuyLevel`, `RsiSellLevel` | RSI の構成としきい値。
| `MacdFast`, `MacdSlow`, `MacdSignal` | MACD パラメータ。
| `BollingerLength`, `BollingerWidth` | Bollinger バンド設定。
| `StochasticLength`, `StochasticK`, `StochasticD` | Stochastic オシレーターのパラメーター。
| `UseMa`, `UseRsi`, `UseMacd`, `UseBollinger`, `UseStochastic` | 個々のフィルターを切り替えます。
| `CandleType` | 信号評価に使用される時間枠。

## MetaTrader の違い
- 元の EA は、ブローカー固有の制約に対してボリュームを検証します。ポートは、StockSharp の音量ステップにスナップし、可能な場合は最小/最大音量を尊重することでこれをミラーリングします。
- 保護レベルは、手動の価格計算ではなく、StockSharp ヘルパーを介して価格ステップに変換されます。
- すべてのインジケーター値は、`GetValue` を直接呼び出すことなく、高レベル バインディング API を通じて消費されます。

## 使用のヒント
1. 戦略をポートフォリオと証券に添付し、`CandleType` を介して時間枠を構成します。
2. インジケーターの切り替えを調整して、元の EA の動作を再現または簡素化します。
3. 取引回数を減らす必要がある場合は、`MinTradeIntervalMinutes` を増やします。より頻繁なエントリの場合は、値を減らします。
4. `TakeProfitPoints` と `StopLossPoints` がシンボルの目盛サイズと一致していることを確認します。
