# Fuzzy Logic Legacy 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 StockSharp 中重现 2007 年发布的 MetaTrader "Fuzzy logic" 专家顾问。它把比尔·威廉姆斯的工具与动量振荡指标结
合，并使用模糊评分表来综合判断市场方向。只有当综合得分显示明显的多头或空头共识时，系统才会开仓。固定止损与可选
的移动止损复刻了原始策略的仓位管理方式。

## 交易逻辑

1. 使用平滑移动平均线构建威廉姆斯鳄鱼指标（下颌、牙齿、嘴唇），并计算三条线之间距离绝对值之和作为 *Gator* 指标。
2. 在同一组K线数据上计算 Williams %R（14）、DeMarker（14）和 RSI（14）。
3. 由 Awesome Oscillator 派生加速指标（AC），追踪最多五根连续柱来识别动量加速序列。
4. 将每个指标映射到五级的模糊隶属度表，阈值完全沿用原始代码。
5. 对所有隶属度进行加权求和，得到 0 到 1 之间的决策值：
   - **> 0.75** 代表多头共识，触发买入。
   - **< 0.25** 代表空头共识，触发卖出。
6. 同一时间只允许一笔持仓，入场后立即附加保护性止损。

## 仓位管理

- **止损**：按价格跳动距离设置 (`Stop Loss (points)` 参数)。
- **移动止损**：可选，如启用则按设定的跳动距离跟随。
- **资金管理**：可选，使用 MetaTrader 原公式 `Volume = (Balance * (PercentMM + DeltaMM) - InitialBalance * DeltaMM) / 10000`
  计算下单量。

## 参数

| 参数 | 说明 |
|------|------|
| `Candle Type` | 用于分析的K线类型。 |
| `Long Threshold` | 开多仓所需超过的决策阈值。 |
| `Short Threshold` | 开空仓所需低于的决策阈值。 |
| `Stop Loss (points)` | 初始止损的价格跳动数。 |
| `Trailing Stop (points)` | 移动止损的价格跳动数；填 `0` 表示禁用。 |
| `Fixed Volume` | 关闭资金管理时的固定下单量。 |
| `Use Money Management` | 是否启用 MetaTrader 式的资金管理。 |
| `Percent MM` | 资金管理公式中使用的余额百分比。 |
| `Delta MM` | 资金管理公式中的附加百分比。 |
| `Initial Balance` | 资金管理公式所参考的初始余额。 |

## 备注

- 策略仅在蜡烛收盘后（`CandleStates.Finished`）处理数据，以避免重新绘制。
- 所有阈值与权重均与原版一致，从而保持原有行为模式。
- 如果在日内环境中使用，请根据波动性调整时间框架和阈值。
