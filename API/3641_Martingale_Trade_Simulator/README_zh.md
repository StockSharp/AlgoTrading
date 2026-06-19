# 马丁格尔交易模拟器策略

## 概述

`MartingaleTradeSimulatorStrategy` 在 StockSharp 中重现了 MetaTrader 上的 “Martingale Trade Simulator” 专家顾问。该策略提供手动交易面板功能：即时发送市价单、按键触发马丁格尔加仓、以及在无须额外脚本的情况下管理移动止损。所有开关都通过参数实时响应，非常适合在策略测试器中进行交互式实验。

## 工作原理

### 手动市价按钮
- 参数 `Buy` 与 `Sell` 对应面板上的买入 / 卖出按钮。当参数被设置为 `true` 时，策略会按 `Order Volume` 的数量发送市价单，然后自动把参数重置为 `false`。
- 策略只使用市价单，不挂出任何挂单，完全模拟原 EA 在可视化测试中的行为。

### 马丁格尔加仓
- 启用 `Enable Martingale` 后，可通过把 `Martingale` 参数切换为 `true` 来触发一次加仓检查。
- 策略会根据当前持仓方向判断是否需要加仓：
  - **多头持仓：** 若最新卖价低于已成交买单中的最低价格至少 `Martingale Step (points)`，则发送新的买入市价单。
  - **空头持仓：** 若最新买价高于已成交卖单中的最高价格至少 `Martingale Step (points)`，则发送新的卖出市价单。
- 每一笔加仓的手数等于 `Order Volume × (Martingale Multiplier)^N`，其中 `N` 为当前方向连续入场的次数。
- 一旦进入马丁格尔模式，策略会根据最新的加权平均持仓价重新计算止盈价，并在其基础上加上（或减去）`Martingale TP Offset (points)`，以覆盖累计亏损。

### 移动止损
- 参数 `Enable Trailing` 控制是否启用移动止损。
- 移动止损初始位于距离市场价 `Trailing Stop (points)` 的位置，只有当价格至少向有利方向移动 `Trailing Step (points)` 后才会前移。
- 当市场价触及移动止损时，策略立即发送反向市价单平掉全部仓位。

### 止损与止盈
- `Stop Loss (points)` 与 `Take Profit (points)` 重现了原专家顾问的基础风控选项。
- 多头情况下止损位于平均建仓价下方，止盈位于上方；空头则相反。
- 所有风控都通过市价单执行，确保策略兼容 StockSharp 支持的各类连接器。

## 参数说明

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `Order Volume` | 手动市价单的基础手数。 | `1` |
| `Stop Loss (points)` | 止损距离，设为 0 表示关闭止损。 | `500` |
| `Take Profit (points)` | 止盈距离，设为 0 表示关闭止盈。 | `500` |
| `Enable Trailing` | 是否启用移动止损。 | `true` |
| `Trailing Stop (points)` | 移动止损与价格之间的距离。 | `50` |
| `Trailing Step (points)` | 移动止损前移所需的最小盈利幅度。 | `20` |
| `Enable Martingale` | 允许使用 `Martingale` 按钮进行马丁格尔加仓。 | `true` |
| `Martingale Multiplier` | 每一级加仓的手数乘数。 | `1.2` |
| `Martingale Step (points)` | 触发加仓所需的最小不利价格偏移。 | `150` |
| `Martingale TP Offset (points)` | 重新计算止盈时额外添加的点数。 | `50` |
| `Buy` | 设为 `true` 发送市价买单（自动复位）。 | `false` |
| `Sell` | 设为 `true` 发送市价卖单（自动复位）。 | `false` |
| `Martingale` | 设为 `true` 触发马丁格尔加仓检查（自动复位）。 | `false` |

## 使用步骤

1. 选择交易品种，设置 `Order Volume`，启动策略（可在测试或实盘模式下运行）。
2. 将 `Buy` 或 `Sell` 参数设为 `true`，即可模拟面板上的买入 / 卖出按钮。
3. 首次成交后，当价格向不利方向移动时，把 `Martingale` 参数切换为 `true`，策略会检查是否满足加仓条件并按乘数扩大手数。
4. 结合 `Enable Trailing` 与风险参数，可以完全复刻原 EA 的操作，或尝试不同的实验配置。

## 备注

- 策略依赖 Level1 行情（买一 / 卖一 / 最新成交价）来评估市况。
- C# 源码中的注释均使用英文，以符合仓库规范。
