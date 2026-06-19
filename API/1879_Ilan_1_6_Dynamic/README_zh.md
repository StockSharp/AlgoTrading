# Ilan 1.6 Dynamic 网格策略
[English](README.md) | [Русский](README_ru.md)

Ilan 1.6 Dynamic 是一种经典的网格加仓策略。系统在指定方向上打开首单，当价格以固定步长逆向运动时加仓。每次加仓的手数按 LotExponent 指数增长。当价格回到平均建仓价并达到设定的盈利距离时，整篮子订单一起平仓。如果价格走势良好，可以启用追踪止损保护利润。

该算法完全基于价格，不使用任何指标。由于每次逆向都会增加仓位，风险较高，但可以迅速捕捉反弹。

## 细节

- **入场**
  - 首单按设定方向开仓。
  - 价格每逆向 `PipStep` 点加仓一次，最多 `MaxTrades` 次。
  - 新订单手数 = `InitialVolume * LotExponent^N`。
- **出场**
  - 当价格触及 `AveragePrice ± TakeProfit` 时平掉所有仓位。
  - 可选的追踪止损在盈利达到 `TrailStart` 点后启动，并以 `TrailStop` 点距离跟随。
- **仓位管理**
  - 同时只持有多头或空头的一组仓位。
  - 平仓后策略从初始方向重新开始。
- **参数**
  - `InitialVolume` – 首单手数（默认 1）。
  - `LotExponent` – 后续手数的乘数（默认 1.6）。
  - `PipStep` – 网格层间距点数（默认 30）。
  - `TakeProfit` – 距离平均价的盈利目标点数（默认 10）。
  - `MaxTrades` – 最大持仓订单数（默认 10）。
  - `StartLong` – 若为 true 则先开多单（默认 true）。
  - `UseTrailingStop` – 是否启用追踪止损（默认 false）。
  - `TrailStart` – 启动追踪止损的盈利点数（默认 10）。
  - `TrailStop` – 追踪止损距离点数（默认 10）。
  - `CandleType` – 使用的K线周期（默认 1 分钟）。
- **过滤**
  - 类别: 网格
  - 方向: 双向
  - 指标: 无
  - 止损: 可选
  - 复杂度: 中等
  - 周期: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 高
