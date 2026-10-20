# BTC DCA AHR999

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

使用已完成的 UTC 日线。以最近 `Length` 个正收盘价（含当前）计算 `G=exp(mean(ln(C)))`。BTC 年龄为自 2009-01-03 起的整天数；`F=10^(5.84*log10(age)-17.01)`，`AHR999=(C/G)*(C/F)`。窗口必须完整。

在日期范围内的星期一，指数严格低于 0.45 时按 `UsdInvest2` 预算买入，否则严格低于 1.2 时使用 `UsdInvest1`。其余指数、星期及日期不买入。UTC 日期边界包含在内，每根日线最多决策一次。数量为 `floor(budget/C/VolumeStep)*VolumeStep`，不足最小数量则跳过。预算按信号收盘价计算，实际市价成交可能不同。未完成订单阻止重复提交。持仓累积，没有自动退出。

## 参数

| 参数 | 默认值 |
|---|---|
| `CandleType` | `1d` |
| `Length` | `200` |
| `UsdInvest1` | `100` |
| `UsdInvest2` | `1000` |
| `StartDate` | `2024-02-01T00:00:00Z` |
| `EndDate` | `2025-12-31T00:00:00Z` |

## 参考资料

- [AHR999 geometric-cost implementation](https://github.com/alibaba-web3/quant-master/blob/main/ahr999_index.py)
