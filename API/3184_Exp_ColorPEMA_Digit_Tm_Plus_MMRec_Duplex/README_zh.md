# Exp Color PEMA Digit TM Plus MMRec Duplex (C#)

## 概述
该策略在 StockSharp 平台上重现了 “Exp_ColorPEMA_Digit_Tm_Plus_MMRec_Duplex” 智能交易系统，并使用高层 API 实现。多头与空头模块各自维护一条五重指数移动平均线（PEMA）序列，可以分别设置时间框架与价格来源。当 PEMA 的斜率转为向上时开多单，斜率转为向下时开空单，同时提供基于时间的强制离场保护。

## 指标
* **五重 EMA**：串联八条相同周期的指数平均线，并按照 8、-28、56、-70、56、-28、8、-1 的系数组合。指标值返回当前与前一根数据，方便判断斜率方向。
* **颜色逻辑**：将斜率映射为三种状态（上升、下降、震荡），与原始 ColorPEMA 指标的配色保持一致。

## 信号逻辑
### 多头模块
1. 等待指定多头时间框架的蜡烛收盘。
2. 按配置的价格类型与保留小数计算 PEMA。
3. 读取 `SignalBar` 根之前的颜色状态并与上一根比较。
4. **入场**：当颜色切换为 `Up` 且允许开仓时，按 `TradeVolume` 买入并记录进场时间。
5. **出场**：当颜色变为 `Down` 且允许根据指标离场时，平掉多头。
6. **时间保护**：若持仓时间超过 `LongTimeExitMinutes`，无论指标状态如何都强制平仓。

### 空头模块
与多头模块独立运作：
1. 监听空头时间框架的收盘蜡烛。
2. 计算空头 PEMA 序列。
3. 检查 `ShortSignalBar` 根之前的颜色是否变为 `Down`。
4. **入场**：在允许开仓时做空。
5. **出场**：当颜色回到 `Up` 且允许离场时回补空头。
6. **时间保护**：超过 `ShortTimeExitMinutes` 后强制平仓。

## 风险控制
* `TradeVolume` 控制下单数量。
* `StopLossSteps`、`TakeProfitSteps` 以价格步长为单位设置止损、止盈，只要其中任意一项大于零就会启用 `StartProtection`，与原 MQL 版本的资金管理保护保持一致。
* 多头与空头的持仓计时器独立配置，避免长时间占用资金。

## 参数说明
| 参数 | 说明 |
|------|------|
| `LongCandleType` | 多头指标使用的时间框架。 |
| `ShortCandleType` | 空头指标使用的时间框架。 |
| `LongEmaLength`、`ShortEmaLength` | PEMA 平滑周期（支持小数）。 |
| `LongPriceMode`、`ShortPriceMode` | 价格来源（收盘、开盘、最高、最低、中位、典型、加权、简化、四分位、趋势跟随、Demark）。 |
| `LongDigits`、`ShortDigits` | PEMA 结果的保留小数位。 |
| `LongSignalBar`、`ShortSignalBar` | 回溯的已完成蜡烛数量，用于判断颜色变化。 |
| `LongAllowOpen`、`ShortAllowOpen` | 是否允许开多 / 开空。 |
| `LongAllowClose`、`ShortAllowClose` | 是否允许根据指标离场。 |
| `LongAllowTimeExit`、`ShortAllowTimeExit` | 是否启用时间强制平仓。 |
| `LongTimeExitMinutes`、`ShortTimeExitMinutes` | 多头 / 空头最大持仓时间（分钟）。 |
| `TradeVolume` | 默认下单手数。 |
| `StopLossSteps`、`TakeProfitSteps` | 以价格步长表示的止损与止盈距离。 |

## 其他说明
* 当多头与空头选择相同的时间框架时，StockSharp 会自动复用同一份蜡烛数据。
* 两个模块使用相同的交易品种与下单量设置，但其信号逻辑完全独立。
* 所有计算均在收盘蜡烛上执行，避免出现重绘现象。
