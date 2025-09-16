# Parabolic SAR First Dot 策略

## 概述
**Parabolic SAR First Dot Strategy** 是对 `MQL/9954` 目录中 MetaTrader 专家顾问 `pSAR_bug_4` 的高层 API 迁移。策略关注 Parabolic SAR 第一次跳到价格另一侧的时刻：当 SAR 点位从价格上方翻到下方时买入，从下方翻到上方时卖出。每笔仓位都使用固定的止损和止盈距离进行保护，与原始 MQL 版本保持一致。

## 交易逻辑
1. **数据与指标准备**：策略订阅可配置的蜡烛类型（默认 15 分钟），并以用户设置的加速步长与最大加速度计算 Parabolic SAR。
2. **状态记录**：在第一根完整蜡烛上记住 SAR 相对收盘价的位置，后续蜡烛持续比较新的位置与之前的状态。
3. **入场条件**：
   - **做多**：SAR 从收盘价上方翻到下方。若存在空头仓位，先平仓再按配置的手数买入。
   - **做空**：SAR 从收盘价下方翻到上方。若存在多头仓位，先平仓再按配置的手数卖出。
4. **保护水平**：开仓后立即保存止损与止盈价位，距离等于 `StopLossPoints` 或 `TakeProfitPoints` 乘以合约的 `PriceStep`。当 `UseStopMultiplier` 为真时（默认，与 MetaTrader 的 `StopMult` 一致）距离会再乘以 10，以适应带有小数点报价的经纪商。
5. **离场规则**：每根完整蜡烛检查最高价与最低价，一旦触及止损或止盈就以市价平仓。若出现相反的 SAR 信号，则以足够的数量反手：既平掉当前头寸，又建立新的反向仓位。

## 风险控制
- 每次开仓都会重新计算止损与止盈价格。
- 如果品种没有提供 `PriceStep`，策略会退回使用 `0.0001`，避免出现零距离。
- 所有操作都依赖 `IsFormedAndOnlineAndAllowTrading()`，保证数据形成、连接正常并允许交易。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `TradeVolume` | `0.1` | 新开仓位的手数，同时会同步到 `Strategy.Volume`。 |
| `StopLossPoints` | `90` | Parabolic SAR 点数形式的止损距离，转换为价格时乘以 `PriceStep`（若启用 `UseStopMultiplier` 还会乘以 10）。 |
| `TakeProfitPoints` | `20` | 以 Parabolic SAR 点数表示的止盈距离。 |
| `UseStopMultiplier` | `true` | 是否把止损和止盈距离再乘以 10，用于复刻原脚本的 `StopMult` 逻辑。 |
| `SarAccelerationStep` | `0.02` | Parabolic SAR 的初始加速因子。 |
| `SarAccelerationMax` | `0.2` | Parabolic SAR 的最大加速因子。 |
| `CandleType` | `15 分钟` | 参与计算与触发信号的蜡烛类型。 |

## 转换说明
- MetaTrader 中的止损与止盈是由经纪商托管的挂单；在 StockSharp 中通过监控蜡烛高低价并在突破时发送市价单来模拟。
- 原策略在 `StopMult` 为真时会把距离乘以 10，以适应带小数点报价的经纪商。本实现通过 `UseStopMultiplier` 保留了该开关。
- 代码完全使用高层 API（`SubscribeCandles`、`Bind`、`BuyMarket`、`SellMarket`），遵循项目要求。本任务不创建 Python 版本。 
