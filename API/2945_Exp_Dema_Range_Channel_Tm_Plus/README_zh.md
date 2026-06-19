# Exp DEMA Range Channel Tm Plus 策略
[English](README.md) | [Русский](README_ru.md)

Exp DEMA Range Channel Tm Plus 策略将原版 MetaTrader 专家顾问迁移到 StockSharp 高级 API。它在每根 K 线的高点和低点上分别计算双指数移动平均线（DEMA），形成一个价格通道，然后根据通道生成的颜色判断是否出现向上或向下的突破。策略保留了原始脚本中的突破与持仓时间规则，同时将资金管理简化为使用 `Volume` 属性与可选的保护性止损/止盈。

## 工作原理

- **通道构建**
  - 使用同一周期 `MaPeriod` 计算高点 DEMA 与低点 DEMA。
  - 通道数值向前平移 `Shift` 根 K 线，以复制原始指标的绘图方式，因此比较的是若干根 K 线之前的 DEMA 值。
  - `PriceShiftPoints` 提供额外的点数偏移，用于扩大或收窄通道。
- **颜色与信号**
  - 如果收盘价高于平移后的上轨，则认为出现多头突破；根据 K 线实体方向确定颜色（原脚本的索引 2 或 3）。
  - 如果收盘价低于下轨，则认为出现空头突破；颜色对应原脚本的索引 0 或 1。
- **入场条件**
  - 策略维护颜色队列并回溯 `SignalBar` 根 K 线来寻找最新信号，同时确认上一根颜色不同，以捕捉“新”突破。
  - 当 `EnableBuyEntry` 为真且出现上行突破时，可开多单。
  - 当 `EnableSellEntry` 为真且出现下行突破时，可开空单。
- **离场条件**
  - `EnableBuyExit` 为真时，任何新的下行突破都会触发平多。
  - `EnableSellExit` 为真时，新的上行突破会触发平空。
  - `UseHoldingLimit` 与 `HoldingMinutes` 组合实现持仓时间限制：持仓超过设定分钟数将被强制平仓。
- **风险管理**
  - 设置 `StopLossPoints` 与 `TakeProfitPoints` 后，策略会调用 `StartProtection`，根据价格步长换算成实际价格偏移，使用市价方式执行止损或止盈。

## 参数

| 参数 | 说明 |
| --- | --- |
| `MaPeriod` | 上轨与下轨的 DEMA 周期。 |
| `Shift` | 通道数值向前平移的 K 线数量。 |
| `PriceShiftPoints` | 以点数（`PriceStep` 的倍数）表示的额外偏移。 |
| `SignalBar` | 回溯多少根 K 线判断信号（0 表示当前，1 表示上一根等）。 |
| `EnableBuyEntry` / `EnableSellEntry` | 分别控制是否允许做多或做空。 |
| `EnableBuyExit` / `EnableSellExit` | 控制是否在反向信号出现时平多或平空。 |
| `UseHoldingLimit` | 是否启用持仓时间限制。 |
| `HoldingMinutes` | 允许的最大持仓时间（分钟），设置为 0 可在保持开关为真的情况下禁用超时。 |
| `StopLossPoints` / `TakeProfitPoints` | 止损/止盈距离（点数），大于零时会触发保护逻辑。 |
| `CandleType` | 使用的 K 线类型与周期（默认 8 小时，与原始脚本一致）。 |

## 执行流程

1. 订阅 `CandleType` 指定的 K 线，并初始化高低 DEMA 指标。
2. 将最新的通道数值存入队列，保证能够访问到 `Shift` 根 K 线之前的值。
3. 每当 K 线收盘时计算新的颜色，更新颜色缓冲区，并根据 `SignalBar` 判断是否出现新的突破。
4. 若出现反向信号或超出持仓时间限制，则执行平仓。
5. 下单时使用 `Volume + |Position|` 的数量发送市价单，以便在反向信号时自动反手。
6. 更新 `_positionOpenedTime`，确保持仓时间计算正确。

## 使用提示

- 需要保证输入的 K 线数据按时间顺序排列，否则平移后的比较会失真。
- 启动前请设置策略的 `Volume`，因为本移植版本不包含原专家的复杂资金管理逻辑。
- 若在真实账户运行，建议显式配置止损和止盈，防止极端行情造成损失。
- 策略会在图表区域绘制 K 线与成交，方便验证突破信号与实际交易是否一致。
