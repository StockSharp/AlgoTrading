# Williams Alligator ATR

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

三条 Alligator 线均平滑中间价 (high+low)/2；Length、Shift 控制平滑及完整蜡烛延迟。Teeth 必须计算并形成，但不另加开仓过滤。全部线和 Wilder ATR 形成后，Lips−Jaw 从 <=0 跨到 >0 时空仓买入 Volume，从 >=0 跨到 <0 时平掉真实全部多仓，从不做空。入场固定 ATR×AtrMultiplier 为距真实成交价的原生本地市价止损距离，买卖报价可在蜡烛之间触发。无冷却、止盈或随后 ATR 移动止损。

## 参数

| 参数 | 默认值 |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `JawLength` | 13 |
| `TeethLength` | 8 |
| `LipsLength` | 5 |
| `JawShift` | 8 |
| `TeethShift` | 5 |
| `LipsShift` | 3 |
| `AtrPeriod` | 14 |
| `AtrMultiplier` | 2 |

## 验证

两种语言使用随附 BTC/TON，检查独立公式、订单方向/数量、真实成交、仓位与退出。带日期的价格、因子、日历数据验证机制，重定位 OHLC/成交量不是多年历史。两个排名组、TopN=1、SMA=2 是明确的小证券池测试配置，长窗口和原始默认值另行检查。不承诺盈利。
