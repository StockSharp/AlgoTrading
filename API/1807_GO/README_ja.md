# GO戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、始値・高値・安値・終値の指数移動平均（EMA）に出来高を掛け合わせた複合的な**GO**値を計算します。GO値の符号とレベルに基づいてトレードの決定を行います。

## 計算式

`GO = ((C - O) + (H - O) + (L - O) + (C - L) + (C - H)) * V`

ここで:
- `C`, `O`, `H`, `L` – 終値、始値、高値、安値のEMA値。
- `V` – 処理されたローソク足の出来高。

## トレードルール

- **ロング開始**: GO > `OpenLevel`
- **ショート開始**: GO < `-OpenLevel`
- **ロング決済**: GO < (`OpenLevel` - `CloseLevelDiff`)
- **ショート決済**: GO > -(`OpenLevel` - `CloseLevelDiff`)

## パラメーター

| 名前 | 説明 |
|------|------|
| `MaPeriod` | 価格平滑化のためのEMA期間。 |
| `OpenLevel` | 新規ポジションをトリガーするGOレベル。 |
| `CloseLevelDiff` | オープンレベルとクローズレベルの差。 |
| `ShowGo` | GO値をログに記録するかどうか。 |
| `CandleType` | 処理に使用するローソク足の種類。 |

この戦略は確定したローソク足で動作し、ポジション管理には成行注文を使用します。
