# Martingale Trade Simulator 策略

## 概述

**Martingale Trade Simulator Strategy** 将 MetaTrader 5 的手动测试脚本 "Martingale Trade Simulator" 迁移到 StockSharp 高级 API。策略在启动后根据 `InitialDirection` 参数发送第一笔市价单，并自动完成后续管理：

- 当价格朝不利方向移动到达设定距离时，按马丁倍数加仓。
- 为整组持仓重新计算统一的止盈价格，使总盈亏接近保本并留出缓冲。
- 启用与原始 EA 相同的跟踪止损模块。
- 依据 `CandleType` 指定的任意周期蜡烛执行所有判断。

该转换保留了原脚本的风险管理和加仓逻辑，同时可以直接在 StockSharp 生态中运行。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `InitialVolume` | 首笔交易的基础手数。 | `0.01` |
| `StopLossPips` | 以点（pip）表示的统一止损距离。 | `500` |
| `TakeProfitPips` | 仅有一笔持仓时使用的止盈距离。 | `500` |
| `TrailingStopPips` | 激活跟踪止损所需的盈利距离。 | `50` |
| `TrailingStepPips` | 跟踪止损每次向前移动的步长。 | `20` |
| `LotMultiplier` | 每次加仓时的马丁手数倍增系数。 | `1.2` |
| `StepPips` | 触发下一次加仓所需的价格回撤距离。 | `150` |
| `TakeProfitOffsetPips` | 当存在多笔仓位时改用的止盈偏移。 | `50` |
| `EnableMartingale` | 是否启用马丁加仓。 | `true` |
| `EnableTrailing` | 是否启用跟踪止损管理。 | `true` |
| `InitialDirection` | 首笔订单方向（`None`、`Buy`、`Sell`）。 | `None` |
| `CandleType` | 驱动策略运行的蜡烛类型。 | `1 分钟` |

## 交易流程

1. **初始入场**：启动时按照 `InitialDirection` 下达市价单；如果为 `None`，则等待人工触发。
2. **马丁加仓**：当价格逆向移动 `StepPips` 时，以 `LotMultiplier^n` 放大手数补仓，并将总止盈目标移动至 `TakeProfitOffsetPips` 位置。
3. **跟踪止损**：在盈利达到 `TrailingStopPips` 后启动跟踪止损，并按 `TrailingStepPips` 步长逐步上调。
4. **统一止损止盈**：策略维护组合级别的止损与止盈，价格触及任意一侧即平掉全部仓位。

## 使用建议

- 策略针对单一净头寸模式设计，适合在实验或回测环境中模拟原脚本的手工流程。
- 请确认证券的 `PriceStep`、`VolumeStep`、`VolumeMin`、`VolumeMax` 等属性已正确设置，以便正确归一化价格与手数。
- 可根据需要调整 `CandleType`，短周期接近逐笔行情，长周期则降低管理频率。

## 可视化

若界面中存在图表区域，策略会绘制指定蜡烛并标记成交，与原始测试面板的视觉反馈保持一致。

## 转换说明

- 原脚本中的按钮交互被参数 `InitialDirection` 所取代。
- 关于保证金与手数的检查使用了 StockSharp 自带的手数归一化逻辑。
- 加仓与跟踪算法改写为适配 StockSharp 的组合持仓模型。
