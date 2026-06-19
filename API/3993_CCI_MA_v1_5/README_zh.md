# CCI MA v1.5 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 中的 “CCI_MA v1.5” 智能交易系统迁移到 StockSharp 的高层 API。原始版本等待商品通道指数（CCI）与基于自身数值的简单移动平均线产生交叉，并借助第二个 CCI 监控 ±100 附近的超买/超卖回撤。移植后的实现保留了相同的信号顺序、可选的资金管理以及以点为单位的止损/止盈规则，并通过蜡烛订阅与指标绑定完成计算。

## 工作原理
* **数据来源** – 用户可以配置任意蜡烛序列（默认 15 分钟）。两个 CCI 都使用蜡烛的收盘价，复现 MetaTrader 中的 `PRICE_CLOSE` 选项。
* **核心指标** – 主 `CommodityChannelIndex`（参数 `CciPeriod`）衡量动量。长度为 `MaPeriod` 的 `SimpleMovingAverage` 作用于 CCI 序列，得到信号线。第二个 CCI（`SignalCciPeriod`）监控 ±100 区域的离场条件。
* **开仓逻辑** – 向上交叉后的下一根蜡烛触发买入：上一根已完成蜡烛的 CCI 必须高于其 SMA，而再往前一根蜡烛的 CCI 位于 SMA 之下。做空逻辑与之对称。若当前持有反向仓位，策略会在下单时加上其绝对数量，实现与 MQL 版本一致的反手行为。
* **平仓逻辑** – 多头在监控 CCI 从 +100 上方跌破 +100，或主 CCI 从上向下穿越其 SMA（同样基于前两根已完成蜡烛）时退出。空头条件相反。保护性止损与止盈按照 MetaTrader 的点数规则实现：策略根据交易品种的 `PriceStep` 计算点值（对于三位或五位报价乘以 10），并在每根完成的蜡烛上比较最高/最低价是否触及 `入场价 ± 距离`。
* **仓位规模** – `LotVolume` 指定基础下单量。当 `UseMoneyManagement` 为真时，策略将其乘以 `floor(balance / DepositPerLot)`，并受 `MaxMultiplier` 限制，完整复刻原程序的资金阶梯。提交订单前会将数量与交易所的 `VolumeStep`、`MinVolume` 和 `MaxVolume` 约束对齐。

## 参数
- **Candle Type** – 指定用于计算指标的蜡烛数据类型。
- **CCI Period** – 主 CCI 的周期长度。
- **Exit CCI Period** – 监控 ±100 阀值的辅助 CCI 周期。
- **CCI MA Period** – 作用于主 CCI 的简单移动平均线周期。
- **Lot Volume** – 资金管理前的基础下单量。
- **Enable Money Management** – 是否开启基于账户余额的仓位扩展。
- **Deposit Per Lot** – 每增加一手所需的余额增量（仅在启用资金管理时使用）。
- **Max Multiplier** – 资金管理可达到的最大倍数。
- **Stop Loss (pips)** – 以点为单位的止损距离（0 表示关闭）。
- **Take Profit (pips)** – 以点为单位的止盈距离（0 表示关闭）。

策略会在至少收集到两根完整蜡烛后才开始交易，以便完全复现 MQL 中基于前两根蜡烛的比较逻辑。止损与止盈检查在每根已完成蜡烛上执行，并利用其最高价/最低价来近似 MetaTrader 服务器端的保护性订单，同时保持在 StockSharp 高层 API 范围内运行。
