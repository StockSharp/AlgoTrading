# Ichimoku Chinkou クロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Ichimoku の Chinkou Span（遅行スパン）と価格のクロスオーバーに基づいて取引します。

## 戦略ロジック

- **ロング:** Chinkouが価格を下から上に抜け、現在の価格とChinkouの両方がKumo雲の上にあり、RSIが`RsiBuyLevel`を上回っている場合。
- **ショート:** Chinkouが価格を上から下に抜け、現在の価格とChinkouの両方がKumo雲の下にあり、RSIが`RsiSellLevel`を下回っている場合。

この戦略は`StartProtection`によるストップロス保護を使用し、Tenkan、Kijun、Senkou Span B、RSIのパラメーターを持ちます。

## パラメーター

| 名前 | 説明 | デフォルト |
|------|------|-----------|
| `TenkanPeriod` | 転換線の期間 | 9 |
| `KijunPeriod` | 基準線の期間 | 26 |
| `SenkouSpanPeriod` | 先行スパンBの期間 | 52 |
| `RsiPeriod` | RSI計算期間 | 14 |
| `RsiBuyLevel` | ロングのRSI最小値 | 70 |
| `RsiSellLevel` | ショートのRSI最大値 | 30 |
| `StopLoss` | ストップロスのパーセントまたは値 | 2% |
| `CandleType` | 購読するローソク足タイプ | 5分足 |

## インジケーター

- Ichimoku
- RSI
