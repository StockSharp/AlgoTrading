# Auto Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

既存のポジションにストップロスとテイクプロフィット注文を自動的に付加し、価格が有利な方向に動くにつれてストップをトレールします。

## 詳細
- **エントリー条件**: なし。この戦略はトレードを開始しません。
- **ロング/ショート**: すでに建てられているロングとショート両方のポジションに対応します。
- **エグジット条件**: ストップロスとテイクプロフィット注文。価格がトレーリング距離の半分移動した後にトレーリングストップが更新されます。
- **ストップ**: ポジションが出現したときに初期ストップロスとテイクプロフィットが設定され、ストップロスは`TrailingStopStep`でトレールします。
- **デフォルト値**: TrailingStop 6, TrailingStopStep 1, TakeProfit 35, StopLoss 114.
- **フィルター**: パラメーターによるトレーリングストップ、自動テイクプロフィット、または自動ストップロスのオプション無効化。

## パラメーター
- `FridayTrade` - 金曜日のトレーリングを許可する。
- `UseTrailingStop` - トレーリングストップロジックを有効にする。
- `AutoTrailingStop` - 真の場合はデフォルトのトレーリング距離6を使用する。
- `TrailingStop` - AutoTrailingStopが偽の場合の価格単位でのトレーリング距離。
- `TrailingStopStep` - トレーリングストップを移動する前の最小価格移動量。
- `AutomaticTakeProfit` - テイクプロフィット注文を自動的に設置する。
- `TakeProfit` - テイクプロフィット距離。
- `AutomaticStopLoss` - ストップロス注文を自動的に設置する。
- `StopLoss` - ストップロス距離。
- `CandleType` - 価格更新に使用するローソク足の種類（デフォルト1分）。
