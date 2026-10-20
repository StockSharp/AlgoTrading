# Two X Spy Tips

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

SpySecurity、TipsSecurity 为 Universe 中两个不同的明确指定信号，交易 Security 也在其中。每月第一根同步完整蜡烛上，两者最新完整日收盘价必须严格高于各自包含该收盘价的 SmaLength 日 SMA。已充分预热但未通过的过滤条件消耗当月审查；预热不足可延迟。空仓时买入按 VolumeStep 向下取整的 Capital×Leverage/价格。持有多头，无退出、做空或每月追加。金额和价格使用美元，遵守 MinTradeUsd 及原生数量限制。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | 100000 |
| `Leverage` | 2 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(1440).TimeFrame() |
| `PriceHistoryFile` | 未提供 |
| `SpySecurity` | 必填 |
| `TipsSecurity` | 必填 |
| `SmaLength` | 200 |

## 输入数据

可选 `PriceHistoryFile` CSV：`SecurityId,Date,Close`，收盘价为正，证券/日期唯一，日期 `yyyy-MM-dd`。日内仅用当前 UTC 日期之前的数据，完整日蜡烛可计入自身收盘。未来行不得提前影响决策，执行蜡烛替换当日值。

## 验证

两种语言使用随附 BTC/TON，检查独立公式、订单方向/数量、真实成交、仓位与退出。带日期的价格、因子、日历数据验证机制，重定位 OHLC/成交量不是多年历史。两个排名组、TopN=1、SMA=2 是明确的小证券池测试配置，长窗口和原始默认值另行检查。不承诺盈利。
