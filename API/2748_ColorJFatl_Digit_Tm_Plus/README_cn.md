# ColorJFatl Digit TM Plus 策略

## 概述

ColorJFatl Digit TM Plus 策略来源于 MetaTrader 5 的
*Exp_ColorJFatl_Digit_Tm_Plus* 专家顾问，并在 StockSharp 平台上重新实现。
它基于 FATL（Fast Adaptive Trend Line）数字滤波器和 Jurik 移动平均线
的组合。原始指标会输出三种颜色（上升、平稳、下降），策略在最近
一根已完成 K 线的颜色发生变化时，按照新的斜率方向调整仓位。

本移植版本保持了 MQL 策略的主要行为：只在收盘 K 线上产生信号，
支持时间退出，同时通过 `TradeVolume` 参数控制下单数量。

## 信号逻辑

1. **指标计算**
   - 价格首先通过 39 阶的 FATL 数字滤波器。
   - 然后使用 Jurik 移动平均线进行平滑处理，可通过参数设置长度、
     价格类型及四舍五入精度。
   - 当前平滑值与前一值的差分决定颜色：`2` 表示上升斜率，`0`
     表示下降斜率，`1` 表示斜率未变。

2. **入场条件**
   - **做多**（`EnableBuyEntries`）在当前颜色变为 `2` 且前一颜色小于
     `2` 时触发。若 `EnableSellExits` 为真，则在开多之前先平掉空头。
   - **做空**（`EnableSellEntries`）在当前颜色变为 `0` 且前一颜色大于
     `0` 时触发。若 `EnableBuyExits` 为真，则在开空之前先平掉多头。
   - 同一时间只允许持有一个方向的仓位，订单在确认 K 线收盘时发出。

3. **离场条件**
   - **斜率反转**：当颜色翻转时，根据 `EnableBuyExits` 或
     `EnableSellExits` 自动平仓。
   - **时间退出**：若启用 `UseTimeExit`，持仓时间达到
     `HoldingMinutes` 分钟后强制离场。
   - **保护价格**：`StopLossPoints` 和 `TakeProfitPoints` 以价格步长为
     单位，策略在每根 K 线上比较最高价/最低价与入场价来判断是否触发。

## 参数

| 参数 | 说明 |
|------|------|
| `TradeVolume` | 市价单的下单数量。 |
| `StopLossPoints` | 止损距离（价格步长），`0` 表示关闭。 |
| `TakeProfitPoints` | 止盈距离（价格步长），`0` 表示关闭。 |
| `EnableBuyEntries` / `EnableSellEntries` | 启用/禁用多头或空头入场。 |
| `EnableBuyExits` / `EnableSellExits` | 启用/禁用基于斜率变化的出场。 |
| `UseTimeExit` | 是否启用时间退出。 |
| `HoldingMinutes` | 启用时间退出时的持仓分钟数。 |
| `CandleType` | 计算所用的 K 线类型（默认 4 小时）。 |
| `JmaLength` | Jurik 移动平均线的平滑长度。 |
| `AppliedPrice` | 指标使用的价格源（收盘价、开盘价、Demark 等）。 |
| `RoundingDigits` | 平滑线的四舍五入精度。 |
| `SignalBar` | 用于判断信号的已完成 K 线偏移量。 |

## 注意事项

- 策略仅处理已完成的 K 线，适合进行历史回测。
- `AppliedPrice.Demark` 完全复制了原指标中的 Demark 价格公式。
- StockSharp 中订单执行是异步的，因此策略在开仓时记录入场价，在
  发送平仓单时清除记录，以保证止损/止盈逻辑正确运行。
