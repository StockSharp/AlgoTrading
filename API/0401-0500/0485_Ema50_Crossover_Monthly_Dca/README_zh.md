# Ema50 Crossover Monthly Dca

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

使用完整周蜡烛和 EmaLength 收盘价 EMA，以首个完整窗口均值初始化。收盘时间须从 StartDate 起。首次观测到的有效日历月记入一次 DcaAmount，之后按经过月份记入，EMA 预热时亦如此；不虚构首次观测前数十年的投入。高于 EMA 时以原生取整数量投入累计现金，低于时卖掉全部真实多仓，相等则持有。现金账本依据实际成交和卖出总收入，不模拟账户存款或融资。等待订单、遵守数量限制并保留余款。只做多，无 RSI、冷却或保护止损。

## 参数

| 参数 | 默认值 |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(10080).TimeFrame() |
| `EmaLength` | 50 |
| `DcaAmount` | 100000 |
| `StartDate` | 1980-01-01 |

## 验证

两种语言使用随附 BTC/TON，检查独立公式、订单方向/数量、真实成交、仓位与退出。带日期的价格、因子、日历数据验证机制，重定位 OHLC/成交量不是多年历史。两个排名组、TopN=1、SMA=2 是明确的小证券池测试配置，长窗口和原始默认值另行检查。不承诺盈利。
