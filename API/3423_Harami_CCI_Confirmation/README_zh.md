# Harami CCI Confirmation 策略

## 概述
Harami CCI Confirmation 是对 MetaTrader 5 专家顾问 `Expert_ABH_BH_CCI` 的 StockSharp 高级 API 复刻。原始 EA 交易两根 K 线组成的孕线（Bullish Harami / Bearish Harami）反转形态。策略在下单之前，会先通过商品通道指数（CCI）进行确认，并用移动平均线衡量较大实体是否真正主导了区间。该移植版本完整保留确认逻辑、只处理收盘后的 K 线，并启用了 StockSharp 内置的保护模块。

## 策略逻辑
### 形态识别
* **实体均值** – 维护最近 *N* 根（默认 5 根）K 线实体绝对值的滑动平均，对应 MetaTrader 中的辅助类，用于平滑实体大小和趋势判断。
* **Bullish Harami** – 上一根 K 线必须收阳，再往前一根必须收阴且实体长于平均值；阳线的实体需要完全处于阴线实体区间内，同时较早那根 K 线的中点要低于收盘价均线，确认处于下跌趋势。
* **Bearish Harami** – 完全对称：上一根 K 线收阴，更早一根收阳且实体较长；阴线实体落在阳线实体内，并且较早 K 线的中点高于收盘价均线，确认上涨趋势。

### CCI 确认
* **入场过滤** – 使用上一根已完成 K 线的 CCI 值（偏移量 1）。做多需要 CCI 低于 `-EntryThreshold`（默认 50），做空需要 CCI 高于 `+EntryThreshold`。
* **离场带** – 持续监控 CCI 是否穿越 ±`ExitBand`（默认 80）。当 CCI 上穿 `-ExitBand` 时关闭所有空头仓位；当 CCI 下破 `+ExitBand` 时平掉多头仓位。该机制复刻了原始专家顾问中负责平仓的“投票”逻辑。

### 交易管理
* **方向反转** – 如果出现相反方向的确认信号，策略会发送足够的量先平掉当前仓位，再建立新的反向持仓。
* **风险控制** – 调用了 `StartProtection()`，用户可在 StockSharp 界面中附加止损或止盈。为保持与原策略一致，默认不强制设置固定的止损/止盈值。

## 参数
* **Order Volume** – 每次市价单的基础下单量，触发反转时会自动补足反向平仓所需数量。
* **CCI Period** – 商品通道指数的周期。
* **Body Average** – 计算实体平均值与收盘价均线时所使用的历史 K 线数量。
* **CCI Entry** – 接受孕线信号所需的 CCI 最小绝对值。
* **CCI Exit Band** – 定义 CCI 穿越时触发离场的带宽。
* **Candle Type** – 运算所使用的时间框架（默认：1 小时）。

## 其他说明
* 策略通过 `SubscribeCandles` 订阅的收盘 K 线进行计算，忽略盘中信号，以匹配 MetaTrader 的执行方式。
* 仅维护短期历史和 CCI 序列即可完成判断，无需构建完整的指标缓冲区。
* 本目录仅包含 C# 实现，本次移植没有 Python 版本。
