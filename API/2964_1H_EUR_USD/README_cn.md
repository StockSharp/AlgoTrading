# 1小时 EUR/USD MACD 摆动策略
[English](README.md) | [Русский](README_ru.md)

本策略将 MetaTrader 中的 “1H EUR_USD” 专家顾问迁移到 StockSharp 的高级 API。它基于 1 小时 K 线交易 EUR/USD（也可用于其它提供标准蜡烛数据的品种），同时结合两条均线与 MACD 摆动确认。只有在快速均线位于慢速均线上/下、MACD 形成双底/双顶结构并且价格突破近期高低点时才会开仓。风险控制与原始 EA 完全一致：止损、止盈以及分步式追踪止损均以点（pip）为单位设置。

## 细节

- **市场**：默认针对 EUR/USD 的 1 小时周期，可根据需要修改为其它标的或周期。
- **入场条件**：
  - **做多**：
    - 快速均线位于慢速均线上方（均线类型可选 SMA、EMA、SMMA、LWMA，并可设置水平偏移）。
    - MACD 主线在零轴下方形成以下任意一种多头摆动：
      - `MACD[-1] > MACD[-2] < MACD[-3]`，且 `MACD[-2] < 0`，当前收盘价突破上一根 K 线的最高价。
      - `MACD[-2] > MACD[-3] < MACD[-4]`，且 `MACD[-3] < 0`，当前收盘价突破前两根 K 线的最高价。
  - **做空**：
    - 快速均线位于慢速均线下方。
    - MACD 主线在零轴上方形成完全对称的空头摆动，并且价格收于相应低点之下。
- **离场方式**：
  - 开仓后立即设置点差式止损和止盈。
  - 只有当浮动收益超过 `TrailingStop + TrailingStep` 点时，追踪止损才会启动，并以 `TrailingStop` 点的距离跟随价格，完全复刻原始 EA 的分步移动逻辑。
  - 判断是否触发保护性订单时同时参考本根蜡烛的最高价/最低价。
- **仓位管理**：
  - 使用 `TradeVolume` 指定的基础手数；当需要反手时，会先平掉反方向仓位再开新仓。
  - 点值会根据合约价格精度自动放大（例如 3 位或 5 位小数时乘以 10）。
- **指标**：
  - 快速与慢速均线，可选择不同类型并设置水平偏移。
  - 标准 MACD 指标（快速/慢速/信号 EMA 长度）。
- **主要参数**：
  - `TradeVolume`：下单手数。
  - `StopLossPips`、`TakeProfitPips`：止损与止盈的点数（设置为 0 可关闭）。
  - `TrailingStopPips`、`TrailingStepPips`：追踪止损配置；启用追踪时步长必须大于 0。
  - `FastMaLength`、`FastMaShift`、`FastMaType`：快速均线参数。
  - `SlowMaLength`、`SlowMaShift`、`SlowMaType`：慢速均线参数。
  - `MacdFastLength`、`MacdSlowLength`、`MacdSignalLength`：MACD 周期。
  - `CandleType`：计算所用的蜡烛类型（默认 1 小时）。
  - `LookbackPeriod`：保留自原始 MQL 输入，在两边代码中均未实际参与逻辑，仅作为兼容性参数。

## 说明

- 追踪止损严格在收益满足 `TrailingStop + TrailingStep` 后才会前移，并保持固定间距。
- 通过 `Security.PriceStep` 自动推导点值；对于 3 或 5 位小数的货币对，会自动乘以 10 与 MT 版保持一致。
- C# 源码中包含详细的英文注释，便于阅读、维护及扩展。
