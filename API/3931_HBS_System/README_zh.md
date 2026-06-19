# HBS System 策略（StockSharp 版本）

## 概览

**HBS System** 策略是 MetaTrader 4 指标 EA "HBS system.mq4"（ForTrader.ru）的 StockSharp 高阶 API 迁移版本。原版 EA 结合 EMA 趋势过滤和挂单突破思路：在趋势方向上放置两笔止损挂单，第一笔在最近的整数价位获利，第二笔继续追逐更远的突破。两笔交易共享同一个止损与拖尾逻辑。

本次迁移忠实保留多挂单结构，并使用 StockSharp 的 `BuyStop`、`SellStop`、`SellLimit`、`BuyLimit` 等高阶方法来管理交易和风控。代码中的注释均为英文，符合仓库规范。

## 交易逻辑

1. **趋势过滤**：对每根已完成 K 线的中位价 (`(High + Low) / 2`) 计算指数移动平均线 (EMA)，仅在 EMA 已形成时才评估信号，对应 MT4 中 `iMA(..., shift=1)` 的行为。
2. **价格取整**：上一根 K 线的收盘价通过可配置的倍数进行向上/向下取整（默认倍数为 `100`，即两位小数），模拟原脚本中的 `MathCeil`/`MathFloor` 操作。
3. **挂单布局**：
   - 若前一根 K 线的开盘价和收盘价均高于 EMA，则放置两笔 Buy Stop：
     - **主挂单**：价格为 `取整价 − entryOffset`，止盈为取整价。
     - **副挂单**：与主挂单同一入场价，但止盈再向上偏移 `secondaryTakeProfitPoints` 点。
     - 两笔多单共用同一止损 (`entry − stopLossPoints`)。
   - 若前一根 K 线的开盘价和收盘价均低于 EMA，则以镜像方式布置 Sell Stop。
   - 为避免方向冲突，系统会自动取消相反方向的挂单。
4. **仓位管理**：当挂单成交后，策略会登记对应的止盈限价单，并更新共享止损。价格向有利方向移动超过设定距离时，拖尾止损会相应收紧。
5. **状态清理**：已完成或被取消的订单会从内部集合移除；当仓位归零时，会撤销所有保护性订单，策略状态回到初始。

## 参数说明

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `EMA Period` | EMA 趋势过滤的周期。 | 200 |
| `Buy Stop-Loss (points)` | 多单入场价与止损之间的点数距离。 | 50 |
| `Buy Trailing (points)` | 多单拖尾止损的距离。 | 10 |
| `Sell Stop-Loss (points)` | 空单入场价与止损之间的点数距离。 | 50 |
| `Sell Trailing (points)` | 空单拖尾止损的距离。 | 10 |
| `Order Volume` | 每笔挂单的下单量。由于默认两笔挂单，最大敞口约为该数值的两倍。 | 0.1 |
| `Entry Offset (points)` | 挂单相对于取整价的点数偏移。 | 15 |
| `Second Take-Profit (points)` | 第二个止盈目标相对取整价的附加偏移。 | 15 |
| `Rounding Factor` | 取整倍数（例如 100 表示保留两位小数）。 | 100 |
| `Candle Type` | 用于计算信号的蜡烛图数据类型，默认 1 小时。 | `TimeFrame(1h)` |

## 使用提示

- 请确保标的证券设置了 `PriceStep` 或 `Decimals`，否则策略将退回使用 0.0001 作为最小点值。
- 每笔挂单都有独立止盈，因此仓位可能分两次分批离场。
- 拖尾止损只有在浮动盈利超过设定点数后才会启动 (`TrailingStop{Buy/Sell}Points`)。
- 如果品种需要不同的取整精度，请调整 `RoundingFactor`。
- 策略未包含资金管理模块，请根据风险偏好设置 `OrderVolume`。

## 迁移要点

- 代码遵循仓库约定：制表符缩进、命名空间为 `StockSharp.Samples.Strategies`、注释为英文。
- 使用高阶 API 完成蜡烛订阅、挂单注册以及保护性订单维护。
- 两阶段止盈与共用止损的逻辑完整复刻原始 MT4 策略。

## 参考

- 原始 MT4 脚本：`MQL/8134/HBS_system.mq4`
- StockSharp 官方文档：[https://doc.stocksharp.com/](https://doc.stocksharp.com/)
