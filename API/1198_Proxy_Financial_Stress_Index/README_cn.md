# Proxy Financial Stress Index

该策略利用多个市场数据（VIX、美国十年期国债收益率、DXY、S&P 500、EUR/USD 和 HYG）构建代理金融压力指数。每个序列通过 z-score 进行标准化并按权重组合。当指数跌破阈值时开多仓，持有固定数量的 K 线后平仓。

## 入场条件
- 压力指数下穿 `Threshold`。

## 出场条件
- 持仓达到 `HoldingPeriod` 根K线后平仓。

## 参数
- `SmaLength` = 41
- `StdDevLength` = 20
- `Threshold` = -0.8
- `HoldingPeriod` = 28
- `VixWeight` = 0.4
- `Us10yWeight` = 0.2
- `DxyWeight` = 0.12
- `Sp500Weight` = 0.06
- `EurusdWeight` = 0.1
- `HygWeight` = 0.18

## 指标
- SMA
- StandardDeviation
