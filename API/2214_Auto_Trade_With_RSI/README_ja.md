# RSIを使った自動売買戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は直近のRSI値を平均化して取引シグナルを生成します。設定可能な期間で標準的な相対力指数（RSI）を計算し、次にRSI自体に単純移動平均を適用します。平均化されたRSIが事前定義された閾値を越えると取引が開かれ、反対の閾値に達すると閉じられます。

## トレードロジック

1. **RSI計算**
   - インジケーターは `RsiPeriod` を使用してローソク足の終値に基づいてRSIを計算します。
2. **RSI平均化**
   - 直近の `AveragePeriod` 個のRSI値を単純移動平均で平滑化します。
3. **エントリールール**
   - `BuyEnabled` が `true` でポジションが開いていない場合、平均RSIが `BuyThreshold`（デフォルト55）を超えると **買い** 注文が送信されます。
   - `SellEnabled` が `true` でポジションが開いていない場合、平均RSIが `SellThreshold`（デフォルト45）を下回ると **売り** 注文が送信されます。
4. **エグジットルール**
   - `CloseBySignal` が `true` の場合、開いているポジションは反対シグナルで閉じられます:
     - 平均RSIが `CloseBuyThreshold`（デフォルト47）を下回るとロングポジションが閉じられます。
     - 平均RSIが `CloseSellThreshold`（デフォルト52）を上回るとショートポジションが閉じられます。

## パラメーター

- `BuyEnabled` – ロングエントリーを有効または無効にします。
- `SellEnabled` – ショートエントリーを有効または無効にします。
- `CloseBySignal` – 反対のRSIシグナルでのエグジットを許可します。
- `RsiPeriod` – RSI計算の長さ。
- `AveragePeriod` – 平均化に使用するRSI値の数。
- `BuyThreshold` – ロングポジションが開かれる平均RSI値の上限。
- `SellThreshold` – ショートポジションが開かれる平均RSI値の下限。
- `CloseBuyThreshold` – ロングポジションが閉じられる平均RSI値の下限。
- `CloseSellThreshold` – ショートポジションが閉じられる平均RSI値の上限。
- `CandleType` – サブスクリプション用のローソク足タイプ。

## 注記

この戦略はStockSharpの高レベルAPIでバインディングによりインジケーター値を組み合わせる方法を示しています。元のMQLバージョンのトレーリングストップと資金管理機能は簡略化のため省略されています。

