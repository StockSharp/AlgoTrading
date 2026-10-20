# Five Ema NoTouch Breakout

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

EMA 形成后仅处理完整蜡烛。蜡烛完全低于 EMA 时准备突破其高点做多，完全高于时准备跌破其低点做空。先检查旧形态再替换，触及不算突破。只从空仓以 Volume 入场，持仓时清除形态。止损固定于形态的另一端极值；首次真实成交时固定到止损的风险距离及 RewardRisk×风险的止盈距离。本地原生市价保护在蜡烛间监听买卖报价。若成交已越过止损则立即平仓。无冷却期或加仓。

## 参数

| 参数 | 默认值 |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `EmaPeriod` | 5 |
| `RewardRisk` | 3 |

## 验证

两种语言使用随附 BTC/TON，检查独立公式、订单方向/数量、真实成交、仓位与退出。带日期的价格、因子、日历数据验证机制，重定位 OHLC/成交量不是多年历史。两个排名组、TopN=1、SMA=2 是明确的小证券池测试配置，长窗口和原始默认值另行检查。不承诺盈利。
