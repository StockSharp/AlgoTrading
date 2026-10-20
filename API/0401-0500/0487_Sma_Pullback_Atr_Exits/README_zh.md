# Sma Pullback Atr Exits

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

两条收盘价 SMA 与 Wilder ATR 均形成后，在完整蜡烛上仅当收盘 < 快 SMA 且快 SMA > 慢 SMA 时买入；收盘 > 快 SMA 且快 SMA < 慢 SMA 时做空。相等不入场。仅空仓以 Volume 入场并等待未完成订单。信号时固定 SL = ATR×AtrMultiplierSl、TP = ATR×AtrMultiplierTp，原生本地市价保护以真实成交为锚并监听买卖报价。之后 ATR 不移动水平。保护平掉全部余仓，反向信号不翻转现有仓位。

## 参数

| 参数 | 默认值 |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `FastSmaLength` | 8 |
| `SlowSmaLength` | 30 |
| `AtrLength` | 14 |
| `AtrMultiplierSl` | 1.2 |
| `AtrMultiplierTp` | 2 |

## 验证

两种语言使用随附 BTC/TON，检查独立公式、订单方向/数量、真实成交、仓位与退出。带日期的价格、因子、日历数据验证机制，重定位 OHLC/成交量不是多年历史。两个排名组、TopN=1、SMA=2 是明确的小证券池测试配置，长窗口和原始默认值另行检查。不承诺盈利。
