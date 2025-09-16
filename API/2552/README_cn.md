# Brakeout Trader v1
[English](README.md) | [Русский](README_ru.md)

Brakeout Trader v1 是一套基于固定价格水平的突破策略。策略仅监控已经收盘的蜡烛，如果最新收盘价穿越用户设定的突破价位，就建立仓位。收盘价向上突破并且允许做多时开多单，收盘价向下突破并且允许做空时开空单。仓位规模根据风险百分比和止损距离计算，可随着账户权益自动调整。

## 交易逻辑
- 仅处理所选 `CandleType` 的完结蜡烛，未完结的蜡烛会被忽略。
- 保存上一根收盘价以判断是否突破 `BreakoutLevel`。
- **开多条件**：最新收盘价高于 `BreakoutLevel`，上一根收盘价在该水平或以下，且 `EnableLong` 为真。若存在空头仓位，会先平仓后再下多单。
- **开空条件**：最新收盘价低于 `BreakoutLevel`，上一根收盘价在该水平或以上，且 `EnableShort` 为真。若存在多头仓位，会先平仓后再下空单。
- 订单按市价提交。数量计算方式确保从入场价到止损价的潜在损失约等于 `RiskPercent` × 当前账户权益；若无法得到风险仓位，则回退到基础 `Volume`。
- 入场后会记录固定的止损和止盈价位（`StopLossPoints` 与 `TakeProfitPoints` 以 pip 点表示）。价格触及任意价位时立即市价平仓并重置缓存。
- 策略使用净头寸管理，不会同时持有同方向的多笔仓位。

## 仓位管理
- 多头的止损设在入场价下方，空头的止损设在入场价上方。距离为 `StopLossPoints * pip`，其中 pip 按 `Security.PriceStep` 推导，对于价格保留 3 或 5 位小数的品种会乘以 10，与原始 MQL 逻辑一致。
- 止盈以 `TakeProfitPoints` 对称设置。
- 当同一根蜡烛内既可能触发止损又可能触发止盈时，优先检查止损，以模拟服务器端的保守执行顺序。
- 反向信号始终在建新仓之前平掉当前仓位，避免出现对冲头寸。
- 当仓位归零后，缓存的入场价、止损价和止盈价会被自动清空。

## 参数说明
- `BreakoutLevel` – 监控突破的固定价格水平。
- `EnableLong` / `EnableShort` – 控制是否允许开多/开空。
- `StopLossPoints` – 止损距离（pip 点数）。
- `TakeProfitPoints` – 止盈距离（pip 点数）。
- `RiskPercent` – 单笔交易允许承担的账户权益百分比，用于按止损距离计算下单数量。
- `CandleType` – 用于信号计算的蜡烛类型（默认 15 分钟）。
- `Volume` – 在无法按风险计算时使用的基础下单数量。

## 细节
- **进场条件**：最新收盘价向上或向下穿越 `BreakoutLevel`。
- **多空方向**：可双向交易，通过 `EnableLong` 与 `EnableShort` 控制。
- **离场条件**：达到固定止损/止盈或出现反向突破信号。
- **止损类型**：固定距离止损，以 pip 点计量。
- **默认值**：`BreakoutLevel = 0`、`StopLossPoints = 140`、`TakeProfitPoints = 180`、`RiskPercent = 10`、`CandleType = 15 分钟`、`EnableLong = EnableShort = true`。
- **过滤器**：除方向开关外，无其他过滤条件。

## 使用提示
- 请选择 pip 计算方式正确的交易品种；若报价保留 3 或 5 位小数，策略会自动将 pip 乘以 10。
- 需确保账户组合能够提供 `CurrentValue`，否则下单数量会退回到基础 `Volume`。
- 市价单的成交价可能与蜡烛收盘价不同，必要时可适当调整止损和止盈距离以覆盖滑点。
