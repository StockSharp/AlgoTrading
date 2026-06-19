# Color Fisher M11 策略

## 概述
Color Fisher M11 策略复刻了 MetaTrader 5 上的 Exp_ColorFisher_m11 专家顾问。它利用改进版的 Fisher Transform 指标，将柱线划分为五个颜色区间，以突出极端的多头与空头动能。信号会延迟若干根已完成的 K 线执行，并提供开仓、平仓方向独立的开关，方便适配不同的交易偏好。

## 指标原理
策略会实时构建 Color Fisher 指标，步骤如下：

- 在 **Range Periods** 窗口内寻找最高价和最低价。
- 计算当前 K 线的中间价，并使用 **Price Smoothing** 系数（类似 EMA）进行平滑。
- 将结果送入 Fisher Transform，并再用 **Index Smoothing** 进行二次平滑，得到最终振荡值。
- 根据 **High Level** 与 **Low Level** 阈值，把振荡值划分为五个颜色状态：
  - `0` – 高于上阈值的强势多头区。
  - `1` – 位于零轴与上阈值之间的普通多头区。
  - `2` – 零轴附近的中性区。
  - `3` – 位于零轴与下阈值之间的普通空头区。
  - `4` – 低于下阈值的强势空头区。

策略会回溯 `Signal Bar` 根已完成的 K 线评估信号，并保存更早一根的颜色，用于识别刚刚进入极端颜色的时刻，与原始 EA 保持一致。

## 交易规则
- **做多开仓**：启用 `Enable Buy Entry` 时，当延迟后的颜色等于 `0` 且上一状态不为 `0`。若当前持有空头仓位，将先平仓再反手做多。
- **做空开仓**：启用 `Enable Sell Entry` 时，当延迟后的颜色等于 `4` 且上一状态不为 `4`。若当前持有多头仓位，将先平仓再反手做空。
- **多头平仓**：启用 `Enable Buy Exit` 时，只要延迟后的颜色落入 `3` 或 `4`，视为空头占优即刻市价离场。
- **空头平仓**：启用 `Enable Sell Exit` 时，只要延迟后的颜色落入 `0` 或 `1`，视为多头占优即刻市价离场。

策略会记录每个方向下一根 K 线的收盘时间，以避免在同一信号上重复下单，必须等到下一根 K 线完成后才允许再次开仓。

## 风险控制
`Stop Loss (pts)` 与 `Take Profit (pts)` 以品种的最小价格步长为单位，将原策略中的点数距离转换为绝对价格距离。当数值大于零时，通过 `StartProtection` 启用对应的止损或止盈；填入零即可关闭该保护措施。

## 参数
- **Range Periods** – 计算高低价区间的窗口长度，默认 10。
- **Price Smoothing** – Fisher 转换前的平滑系数，范围 0…0.99，默认 0.3。
- **Index Smoothing** – Fisher 转换后的平滑系数，范围 0…0.99，默认 0.3。
- **High Level / Low Level** – 确定强势区间的上下阈值，默认 +1.01 / –1.01。
- **Signal Bar** – 信号延迟的已完成 K 线数量，默认 1。
- **Enable Buy Entry / Enable Sell Entry** – 是否允许开多 / 开空。
- **Enable Buy Exit / Enable Sell Exit** – 是否允许根据指标平多 / 平空。
- **Stop Loss (pts) / Take Profit (pts)** – 以价格步长表示的止损、止盈距离。
- **Candle Type** – 订阅的 K 线类型，默认四小时周期。

## 备注
- 策略使用 StockSharp 的高级绑定接口 `SubscribeCandles().BindEx`，除了最少量的颜色历史外不会存储额外序列数据。
- 本次仅提供 C# 版本，不包含 Python 实现。
- 可在图表上同时绘制价格与 Color Fisher 指标，便于观察信号与交易执行。
