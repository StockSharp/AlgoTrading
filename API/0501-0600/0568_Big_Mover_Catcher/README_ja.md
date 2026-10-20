# Big Mover Catcher

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 取引ルール

確定した 1 分終値と、40 本・母標準偏差 2 倍のボリンジャーバンドを使います。建玉がなければ、上限より上で `Volume` を買い、下限より下で `Volume` を売ります。`TradeDirection = null` は両方向、`Buy` または `Sell` は新規方向を限定します。

既定では五つのフィルターすべてを有効にし、同時確認します。RSI(14) は買いで厳密に 50 超、売りで 50 未満、ADX(28) は 20 以上、`100 * ATR(14) / Close >= 0.05`、終値は EMA(350) の上/下、MACD(12,26,9) ヒストグラムは正/負です。個別に無効化でき、有効な指標だけ形成を待ちます。EMA 無効時に 350 本待つ必要はありません。

確定終値が中央線以下なら買い全量、中央線以上なら売り全量を決済します。`IsForceTakeProfitEnabled` 有効時は、順方向の当該足実体を始値で割った値が `ForceTakeProfitPercent`%（既定 5）以上でも決済します。入場からの利益ではなく当該足の実体で、既定では無効です。決済を優先し、同一足で反転しません。

原生ローカル成行ストップは実際の新規約定から `StopLossPercent`%（既定 2）です。未完了注文は重複を防止します。

ネイティブのローカル保護は銘柄の足と購読した価格更新を使用し、距離は実際の新規約定を基準にします。決済は成行で、約定価格は市場の状況と呼値に依存します。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `BollingerPeriod` | `40` |
| `BollingerDeviation` | `2` |
| `IsRsiFilterEnabled` | `true` |
| `RsiPeriod` | `14` |
| `RsiLongThreshold` | `50` |
| `RsiShortThreshold` | `50` |
| `IsAdxFilterEnabled` | `true` |
| `AdxPeriod` | `28` |
| `MinimumAdx` | `20` |
| `IsAtrFilterEnabled` | `true` |
| `AtrPeriod` | `14` |
| `MinimumAtrPercent` | `0.05` |
| `IsEmaFilterEnabled` | `true` |
| `EmaPeriod` | `350` |
| `IsMacdFilterEnabled` | `true` |
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `TradeDirection` | `null` |
| `StopLossPercent` | `2` |
| `IsForceTakeProfitEnabled` | `false` |
| `ForceTakeProfitPercent` | `5` |
| `CandleType` | `1m` |
