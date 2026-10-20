# BTC DCA AHR999

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Trading rules

Uses finished UTC daily candles. With the last `Length` positive closes, including the current one, `G=exp(mean(ln(C)))`. Bitcoin age is the whole number of days since 2009-01-03; `F=10^(5.84*log10(age)-17.01)` and `AHR999=(C/G)*(C/F)`. The complete window is required.

On an eligible Monday, buy the `UsdInvest2` budget if the index is strictly below 0.45, otherwise `UsdInvest1` if strictly below 1.2. Other values, weekdays and dates do not buy. The UTC date range is inclusive; at most one decision occurs per daily candle. Quantity is `floor(budget/C/VolumeStep)*VolumeStep`; a sub-minimum order is skipped. Budget uses the signal close, while market fills can differ. Pending orders block new submissions. Holdings accumulate without automatic exits.

## Parameters

| Parameter | Default |
|---|---|
| `CandleType` | `1d` |
| `Length` | `200` |
| `UsdInvest1` | `100` |
| `UsdInvest2` | `1000` |
| `StartDate` | `2024-02-01T00:00:00Z` |
| `EndDate` | `2025-12-31T00:00:00Z` |

## References

- [AHR999 geometric-cost implementation](https://github.com/alibaba-web3/quant-master/blob/main/ahr999_index.py)
