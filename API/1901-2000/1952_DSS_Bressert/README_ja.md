# DSS Bressert戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はDouble Smoothed Stochastic (DSS) Bressertインジケーターを使用します。2本のラインが計算されます：

- **DSSライン** – 指数移動平均で2回平滑化されたストキャスティクス値。
- **MITライン** – 最初の平滑化後の中間値。

これらのラインがクロスするとトレードがオープンします：

- DSSラインがMITラインを上から下に抜けると買い。
- MITラインがDSSラインを上から下に抜けると売り。

## パラメーター

| パラメーター | 説明 |
|-------------|------|
| `EmaPeriod` | EMA平滑化の期間（デフォルト: 8） |
| `StoPeriod` | ストキャスティクスの計算期間（デフォルト: 13） |
| `TakeProfitPercent` | 保護注文のテイクプロフィット率（デフォルト: 2） |
| `StopLossPercent` | 保護注文のストップロス率（デフォルト: 1） |
| `CandleType` | 計算に使用する時間軸（デフォルト: 4時間） |

## 注意事項

- 戦略は完成したローソク足のみで動作します。
- 保護機能はパーセンテージベースのストップロスとテイクプロフィットを使用します。
