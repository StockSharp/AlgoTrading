# Triple SMA Crossover 策略

## 概述
Triple SMA Crossover 策略复刻了原始 MQL 专家顾问 `3sma.mq4`。策略基于收盘价计算三条简单移动平均线（SMA），当短期趋势与中长期趋势一致时进行交易。本次转换保留了原有规则，并使用 StockSharp 的高级策略 API 实现。

## 交易逻辑
1. 计算三个可配置周期的 SMA。
2. 当快线 SMA 下穿中线 SMA 时平掉多头仓位。
3. 当快线 SMA 上穿中线 SMA 时平掉空头仓位。
4. 当满足以下条件时开多：
   - 快线 SMA 至少高于中线 SMA 指定的价差步数。
   - 中线 SMA 至少高于慢线 SMA 指定的价差步数。
   - 当前没有持有多头仓位。
5. 当满足以下条件时开空：
   - 快线 SMA 至少低于中线 SMA 指定的价差步数。
   - 中线 SMA 至少低于慢线 SMA 指定的价差步数。
   - 当前没有持有空头仓位。

## 参数
- **Candle Type** – 计算移动平均的主要K线周期。
- **Fast SMA Length** – 快速 SMA 的周期（MQL 参数 `SMA1`）。
- **Medium SMA Length** – 中期 SMA 的周期（MQL 参数 `SMA2`）。
- **Slow SMA Length** – 慢速 SMA 的周期（MQL 参数 `SMA3`）。
- **SMA Spread Steps** – 要求 SMA 之间至少相差的价差步数（MQL 参数 `SMAspread`）。
- **Trade Volume** – 开仓时使用的下单量（MQL 参数 `lots`）。

## 说明
- 原脚本中的止损功能被注释，因此在本实现中也没有启用。
- 所有平仓均使用市价单，以贴合原策略的简单行为。
