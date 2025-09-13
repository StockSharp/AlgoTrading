# Volume EA 策略

## 概述
该策略结合成交量激增与商品通道指数 (CCI) 进行交易。当上一根蜡烛的成交量超过更早一根蜡烛的成交量乘以 `Factor` 时，在新一小时的开始开仓。CCI 必须处于指定区间内以确认信号。

## 规则
- 同时只持有一个头寸。
- 每个小时的开始：
  - **做多** 条件：
    - 上一根蜡烛收阳。
    - 上一成交量 > 前一成交量 × `Factor`。
    - CCI 介于 `CciLevel1` 与 `CciLevel2` 之间。
  - **做空** 条件：
    - 上一根蜡烛收阴。
    - 上一成交量 > 前一成交量 × `Factor`。
    - CCI 介于 `CciLevel4` 与 `CciLevel3` 之间。
- 使用 `TrailingStop` 价格步长的移动止损保护利润。
- 当小时数等于 23 时平掉所有仓位。

## 参数
- `Factor` – 成交量倍数阈值。
- `TrailingStop` – 移动止损距离（价格步长）。
- `CciLevel1` / `CciLevel2` – 多头交易的 CCI 区间。
- `CciLevel3` / `CciLevel4` – 空头交易的 CCI 区间。
- `CandleType` – 用于计算的蜡烛周期。
