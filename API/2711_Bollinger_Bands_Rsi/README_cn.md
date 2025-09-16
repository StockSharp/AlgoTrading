# 布林带 RSI 策略
[English](README.md) | [Русский](README_ru.md)

本策略源自 MetaTrader 顾问 “Bollinger Bands RSI”。使用相同周期但不同标准差的三组布林带：黄色带为基准，蓝色带的偏差缩小一半，红色带的偏差扩大一倍。价格回落到指定区域时触发交易，并可叠加 RSI 与随机指标过滤器。

## 策略逻辑
- 黄色带使用 `Deviation` 设置的标准差倍数。
- 蓝色带使用一半倍数，形成较窄的通道；红色带使用两倍倍数，形成更宽的外层通道。
- RSI 与随机指标的数值基于前一根已完成的 K 线（`Bar Shift`），以保持与原始 EA 一致。
- `Only One Position` 控制是否只在空仓时开新单，或者允许在价格回到布林带中轨后继续加仓。

## 入场规则
### 多头
1. 当前 K 线价格下探至 `Entry Mode` 指定的买入区域：
   - 黄色与蓝色之间的中点、蓝色与红色之间的中点，或直接接触其中一条带。
2. 可选过滤条件：
   - RSI：RSI ≤ `100 - RSI Lower`。
   - 随机指标：%K < `100 - Stochastic Lower`。
3. 持仓条件：
   - `Only One Position = true` 时必须在空仓状态下入场。
   - 允许加仓时，只有当 K 线收盘价高于布林中轨后锁定才会被解除。

### 空头
1. 当前 K 线价格上冲至 `Entry Mode` 指定的卖出区域（与多头对称）。
2. 可选过滤条件：
   - RSI：RSI ≥ `RSI Lower`。
   - 随机指标：%K > `Stochastic Lower`。
3. 持仓条件与多头相同（空仓或在收盘价跌破中轨后解除锁定）。

## 出场规则
- `Closure Mode` 决定获利/离场的目标：
  - `Middle Line`：多头触及布林中轨即平仓，空头触及中轨下侧时平仓。
  - `Between Yellow and Blue` / `Between Blue and Red`：使用入场时的相同中点；若入场模式不同，则使用蓝红之间的默认中点。
  - `Yellow Line`、`Blue Line`、`Red Line`：价格触及对应的上/下轨即离场。
- 在加仓模式下，只要收盘价越过中轨，锁定标志会自动清除，允许新的同向交易。

## 风险控制
- `Stop Loss` 与 `Take Profit` 以点数表示，在 `StartProtection` 初始化时通过 `Pip Value` 转换成绝对价格距离。
- 若参数为 0，则不设置相应的止损/止盈。
- `Order Volume` 定义每次市价单的交易量。

## 参数表
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `Entry Mode` | 触发入场的布林带区域。 | 黄蓝之间 |
| `Closure Mode` | 平仓所用的带或中点。 | 蓝红之间 |
| `Bands Period` | 所有布林带的周期长度。 | 140 |
| `Deviation` | 黄色带的标准差倍数（蓝色为一半，红色为两倍）。 | 2.0 |
| `Use RSI Filter` | 启用 RSI 过滤器。 | false |
| `RSI Period` | RSI 计算周期。 | 8 |
| `RSI Lower` | 超买阈值（超卖阈值 = `100 - 值`）。 | 70 |
| `Use Stochastic Filter` | 启用随机指标过滤。 | true |
| `Stochastic Period` | %K 主周期（平滑固定为 3/3 SMA）。 | 20 |
| `Stochastic Lower` | 超买阈值（超卖阈值 = `100 - 值`）。 | 95 |
| `Bar Shift` | 指标向前回看的完成 K 线数量。 | 1 |
| `Only One Position` | 仅在空仓时开仓。 | true |
| `Order Volume` | 每次下单的数量。 | 1 |
| `Pip Value` | 一个点的绝对价格。 | 0.0001 |
| `Stop Loss` | 止损点数（0 表示关闭）。 | 200 |
| `Take Profit` | 止盈点数（0 表示关闭）。 | 200 |
| `Candle Type` | 使用的 K 线类型（默认 1 分钟）。 | 1 分钟 |

## 备注
- 策略仅处理收盘完成的 K 线，因此 `Bar Shift` 应保持 ≥ 1，避免引用未完成数据。
- RSI 与随机指标仅使用 %K 线，%D 线虽然计算但未参与决策，与原 EA 保持一致。
- 代码使用 StockSharp 高级 API 通过 `Bind` 订阅指标，未直接访问指标缓冲区，符合仓库的转换要求。
