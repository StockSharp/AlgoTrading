# iCCI iMA 策略
[English](README.md) | [Русский](README_ru.md)

iCCI iMA 策略源自 MetaTrader 平台的同名专家顾问。算法监控商品通道指数（CCI）与基于 CCI 序列计算的指数移动平均线（EMA）之间的交叉，同时使用第二个 CCI 观察 ±100 区域的超买/超卖反转。订单以手数下达，可选根据账户余额放大，并在点值单位上设置止损与止盈保护。

## 运行原理
* **数据源**：所有指标都使用可配置的蜡烛序列（默认 1 分钟），并取蜡烛的典型价 `(high + low + close) / 3` 作为输入。
* **指标体系**：主 CCI 使用 `CciPeriod` 周期衡量动量；其指数移动平均线（`MaPeriod`）作为信号线对 CCI 进行平滑；辅助 CCI 以 `CciClosePeriod` 周期监控 ±100 水平的突破与回落。
* **入场逻辑**：当当前 CCI 位于 EMA 之上且两根已完成蜡烛之前的 CCI 位于 EMA 之下时，判定为向上交叉并建立多头；当 CCI 向下交叉 EMA 时建立空头。策略仅在所有指标形成且积累两根完整蜡烛后才允许交易，以复现原 MQL 程序的历史比较窗口。
* **出场逻辑**：多头在辅助 CCI 回落到 +100 以下，或主 CCI 从上方跌破 EMA 且两根之前曾位于 EMA 上方时平仓。空头在辅助 CCI 突破 −100，或主 CCI 自下方穿越 EMA 且两根之前位于其下方时平仓。每根已完成蜡烛都会检查止损/止盈：多头触及 `入场价 − stopLossPips * pipSize` 平仓，达到 `入场价 + takeProfitPips * pipSize` 止盈；空头使用 `入场价 + 止损` 与 `入场价 − 止盈` 的对称水平。点值通过证券的最小报价步长计算，对于 3 位或 5 位报价自动乘以 10，与 MetaTrader 的处理一致。
* **仓位管理**：基础手数 (`LotSize`) 会根据交易品种的 `VolumeStep`、`MinVolume` 与 `MaxVolume` 校验。若启用资金管理，策略按 `账户余额 / DepositPerLot` 取整得到的系数放大手数，最大不超过 20，并在每根蜡烛后更新，忠实重现原策略的整数阶梯放大规则。

## 参数
- **Candle Type** – 指定用于计算的蜡烛类型。
- **CCI Period** – 主 CCI 的周期。
- **CCI Close Period** – 监控 ±100 区域的辅助 CCI 周期。
- **CCI EMA Period** – 应用于主 CCI 值的 EMA 周期。
- **Lot Size** – 基础下单手数。
- **Enable Money Management** – 是否启用基于余额的手数放大。
- **Deposit Per Lot** – 每提升一个手数倍数所需的账户余额增量（仅在启用资金管理时生效）。
- **Stop Loss (pips)** – 止损距离（点），为 0 表示关闭。
- **Take Profit (pips)** – 止盈距离（点），为 0 表示关闭。

策略在获得两根完整蜡烛后才开始交易，以确保两根前的比较条件与 MQL 源码一致。止损与止盈通过已完成蜡烛的最高价/最低价近似模拟 MetaTrader 中服务器端的保护单，这一实现符合 StockSharp 高级 API 的工作方式。
