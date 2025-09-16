# Exp Blau CSI 策略

该策略是 MetaTrader 5 专家顾问 `Exp_BlauCSI` 的 C# 版本，利用 Blau Candle Stochastic Index（CSI）对所选 K 线序列进行分析。策略可以基于指标穿越零轴或趋势转折进行交易，并支持以最小价格步长定义的止损和止盈距离。

## 交易逻辑

Blau CSI 将价格动量与最近 K 线的高低价范围进行比较，两者均通过三层移动平均进行平滑处理。

* **Breakdown 模式**：当指标向下穿越零轴时开多，并在前一数值大于零时关闭所有空头；当指标向上穿越零轴时开空，并在前一数值小于零时关闭所有多头。
* **Twist 模式**：当指标出现由跌转涨的拐点时开多，并关闭空头；当指标由涨转跌时开空，并关闭多头。上一柱的方向始终用于管理已有头寸。

所有信号均基于可配置的历史柱（`Signal Bar`）进行确认，以确保使用完整收盘的 K 线。

## 参数

| 参数 | 说明 |
|------|------|
| `Entry Mode` | 选择 `Breakdown` 或 `Twist` 交易逻辑。 |
| `Smoothing Method` | Blau CSI 内部的平滑方式（Simple、Exponential、Smoothed、LinearWeighted、Jurik）。 |
| `Momentum Length` | 计算动量和范围时使用的柱数。 |
| `First/Second/Third Smoothing` | 三层平滑的周期长度。 |
| `Smoothing Phase` | Jurik 平滑的相位参数（其他方法忽略）。 |
| `Momentum Price` / `Reference Price` | 动量领先值与滞后值所使用的价格常量（收盘、开盘、高、低、中价、典型价、加权价、均值价、四分价、趋势价、Demark 等）。 |
| `Signal Bar` | 评估指标时向前回溯的柱数，默认 `1` 表示上一根已收盘 K 线。 |
| `Stop Loss (pts)` | 止损距离，单位为价格步长（`0` 表示禁用）。 |
| `Take Profit (pts)` | 止盈距离，单位为价格步长（`0` 表示禁用）。 |
| `Allow Long/Short Entries` | 控制是否允许开多/开空。 |
| `Allow Long/Short Exits` | 控制是否允许平多/平空信号。 |
| `Candle Type` | 订阅的数据类型（默认 4 小时 K 线）。 |
| `Start Date` / `End Date` | 限制策略参与交易的日期范围。 |
| `Order Volume` | 市价单的下单量。 |

## 风险控制

开仓时根据交易品种的 `PriceStep` 计算止损和止盈价位。如果品种没有提供价格步长，策略会自动禁用止损止盈。策略不包含追踪逻辑，头寸在平仓或达到目标前始终保持初始保护水平。

## 使用说明

1. 将策略连接到能提供所需 `Candle Type` 数据的标的。
2. 按需求设置指标模式和平滑参数。
3. 如需启用止损/止盈，请确保标的具有有效的 `PriceStep`。
4. 通过 `Start Date` 和 `End Date` 可限制策略在指定日期范围内运行。

## 与 MT5 原版的差异

* 使用 StockSharp 指标与策略 API，替代了 MetaTrader 的交易函数。
* 头寸规模控制被简化，直接使用 `Order Volume` 参数。
* 仅支持 StockSharp 提供的平滑方法（Simple、Exponential、Smoothed、LinearWeighted、Jurik），其他 MT5 特定方法自动回退到指数平滑。
* 持仓方向开关与止损止盈逻辑保持与原版一致。

该策略可在 StockSharp Designer、Shell、Runner 或任何自定义的 StockSharp 主程序中进行回测与实盘部署。