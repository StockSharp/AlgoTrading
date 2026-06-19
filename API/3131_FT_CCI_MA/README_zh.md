# FT CCI MA（StockSharp 版本）

## 概述
本策略是 MetaTrader 专家顾问“FT CCI MA”的直接移植。每当一根 K 线收盘时都会评估信号：策略把线性加权移动平均线（LWMA）与 CCI 指标阈值和可选的交易时段过滤器结合使用。StockSharp 实现保留了原始输入名称和默认值，同时利用高级 API（K 线订阅、指标绑定、仓位保护）。

主要实现要点：
- LWMA 使用加权价 `(High + Low + 2 * Close) / 4`，完全对应 MetaTrader 的 `PRICE_WEIGHTED` 选项。
- CCI 使用典型价 `(High + Low + Close) / 3`，与 `PRICE_TYPICAL` 相同。
- 所有逻辑都基于刚刚收盘的 K 线，这与原始 EA 在下一根 K 线的第一笔报价上执行上一根 K 线信号的做法一致。
- `StartProtection` 设置的止盈止损距离以“点（pip）”计量，与 MQL 版本保持一致。

## 交易规则
1. **做多条件**
   - 收盘价高于 LWMA 且 CCI 小于 `CciLevelBuy`（默认 -100），或
   - 收盘价低于 LWMA 且 CCI 小于 `CciLevelDown`（默认 -200）。
   - 只有当当前净头寸为空或为空头时才会触发买入。
2. **做空条件**
   - 收盘价低于 LWMA 且 CCI 大于 `CciLevelSell`（默认 100），或
   - 收盘价高于 LWMA 且 CCI 大于 `CciLevelUp`（默认 200）。
   - 只有当当前净头寸为空或为多头时才会触发卖出。
3. **时间过滤**
   - `UseTimeFilter` 启用后，策略读取 `candle.CloseTime` 的小时部分。
   - 如果当前时间不在交易窗口内，会立即取消所有挂单并平掉持仓。
4. **风险控制**
   - 通过 `StartProtection` 把 `StopLossPips`、`TakeProfitPips` 换算成绝对价格距离，换算基于 `Security.PriceStep`（对 3 或 5 位小数的外汇报价额外乘以 10，使 0.00001 转换为 0.0001 pip）。
   - 下单数量会考虑当前净头寸，因此反向开仓将自动平掉原有头寸。

## 参数
| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `OrderVolume` | 下单手数（lots）。 | `1` |
| `StopLossPips` | 止损距离（pip），0 表示禁用。 | `150` |
| `TakeProfitPips` | 止盈距离（pip），0 表示禁用。 | `150` |
| `UseTimeFilter` | 是否启用交易时段过滤。 | `true` |
| `StartHour` | 开始交易的小时（0–23）。 | `10` |
| `EndHour` | 结束交易的小时（0–23）。若小于 `StartHour`，窗口跨越午夜。 | `5` |
| `CciPeriod` | CCI 指标周期。 | `14` |
| `CciLevelUp` | 激进做空阈值（+200）。 | `200` |
| `CciLevelDown` | 激进做多阈值（-200）。 | `-200` |
| `CciLevelBuy` | 当价格在均线上方时的温和买入阈值（-100）。 | `-100` |
| `CciLevelSell` | 当价格在均线下方时的温和卖出阈值（+100）。 | `100` |
| `MaPeriod` | LWMA 周期。 | `200` |
| `MaShift` | LWMA 水平偏移（单位：K 线）。信号使用 `MaShift` 根之前的均线值。 | `0` |
| `CandleType` | 计算使用的 K 线类型/时间框架。 | `1 小时` |

## 实现细节
- **pip 换算**：优先使用 `Security.PriceStep`。若品种报价保留 3 或 5 位小数，则乘以 10 以匹配 MetaTrader 的 pip 定义。
- **时段过滤**：支持日内窗口（`StartHour < EndHour`）与跨夜窗口（`StartHour > EndHour`）。当二者相等时，策略被禁用，与原版逻辑一致。
- **指标绑定**：通过 `SubscribeCandles().Bind(...)` 获取指标值，无需手工复制缓冲区，仅在内部保存少量 LWMA 历史以处理 `MaShift`。
- **订单管理**：每次下市场单前都会执行 `CancelActiveOrders()`，保证订单簿整洁。
- **无 Python 版本**：按要求仅提供 C# 实现。

## 使用步骤
1. 将策略绑定到目标证券，并设置合适的 `CandleType`（时间框架）。
2. 根据品种规格调整手数和止盈止损的 pip 距离。
3. 按需要开启或关闭交易时段过滤。
4. 启动策略，它会自动订阅 K 线、执行信号并维护保护性订单。

