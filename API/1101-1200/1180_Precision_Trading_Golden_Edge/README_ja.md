# Precision Trading: Golden Edge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAクロスをHullの方向、RSIの勢い、ATRと高値・安値の範囲で確認する戦略です。元の説明は金のスキャルピングを対象としていますが、実装は設定した銘柄を使用し、金での成績を証明しません。

## シグナルと執行

確定した足だけを使用し、既定は5分足です。7つの指標がすべて形成される必要があります。最初の完全に準備された足は前回のEMA/HMAを記録するだけで、エントリーしません。

- ロング：前回の高速EMA ≤ 前回の低速EMA、現在の高速EMA > 現在の低速EMA、現在のHMA > 前回のHMA、RSI > `RsiLongThreshold`（55）。
- ショート：前回の高速EMA ≥ 前回の低速EMA、現在の高速EMA < 現在の低速EMA、現在のHMA < 前回のHMA、RSI < `RsiShortThreshold`（45）。
- 両方向ともcloseとATRが正で、`100 * ATR / close >= MinAtrPercent`（0.05）、かつ`Highest(high) - Lowest(low) >= ATR * RangeAtrMultiplier`（3）が必要です。高値・安値の窓には現在の足を含めます。

条件を満たすクロスで、ポジションがなければ`Volume`を発注し、反対のポジションがあれば`Volume + abs(Position)`で完全に反転します。同方向のシグナルでは維持します。未完了の注文は重複発注を防ぎます。準備された足ごとに前回のEMA/HMAを更新するため、拒否されたクロスを後のエントリーに持ち越しません。6時間の待機期間や追加のストップロス・利確はありません。反対方向の有効なクロスで反転します。

## 指標と既定値

| パラメーター | 既定値 | 意味 |
|---|---:|---|
| `FastLength` / `SlowLength` | 14 / 40 | 標準の終値EMA。高速期間 < 低速期間 |
| `HmaLength` | 55 | `WMA(floor(sqrt(L)), 2 * WMA(floor(L/2), close) - WMA(L, close))`。L ≥ 2 |
| `RsiLength` | 14 | 終値の上昇・下落をWilder平滑化する標準RSI |
| `RsiLongThreshold` / `RsiShortThreshold` | 55 / 45 | 厳密なしきい値。`0 <= short < long <= 100` |
| `AtrLength` | 14 | 前回終値からのギャップを含む標準Wilder ATR |
| `RangeLength` | 20 | 現在の足を含む最高highと最低low |
| `MinAtrPercent` | 0.05 | 終値に対するATRの百分率下限。境界を含み、非負 |
| `RangeAtrMultiplier` | 3 | 高値・安値幅のATR倍率下限。境界を含み、正 |
| `CandleType` | 5分 | 全指標に使う確定足 |

WMAの重みは最古の1から最新の窓長まで増加します。EMAは標準の算術平均から始まり、その後`2 / (length + 1)`を使います。ATRは拡大する窓の平均から始まり、その後Wilder平滑化を使います。期間は正です。Hullは最後の整数平方根期間WMAも形成する必要があります。

C#とPythonのテストは既存BTC/TON履歴からEMA、HMA、RSI、ATR、移動極値を独立に計算します。各注文、完全反転、拒否された確認、6時間未満のシグナル間隔を検証します。収益性や実取引への適合性の検証ではありません。
