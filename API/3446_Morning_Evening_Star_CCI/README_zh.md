# 晨星/暮星 CCI 策略

## 概述
该策略基于 MetaTrader 5 的 **Expert_AMS_ES_CCI** 智能交易系统，在 StockSharp 高级 API 上复刻其逻辑。策略通过识别“晨星”和“暮星”三根蜡烛的反转形态，并结合商品通道指数（CCI）的确认信号，在主图表的已完成K线上做出交易决策。

## 交易逻辑
- **晨星形态做多**
  - 连续三根K线满足晨星特征：
    - 第一根：实体明显向下（实体长度大于平均实体长度）。
    - 第二根：实体很短，并且整体低于第一根K线。
    - 第三根：收盘价高于第一根K线实体的中点。
  - 信号柱的 CCI 数值低于负的入场阈值（默认 −50）。
- **暮星形态做空**
  - 连续三根K线满足暮星特征：
    - 第一根：实体明显向上。
    - 第二根：实体很短，并且整体高于第一根K线。
    - 第三根：收盘价低于第一根K线实体的中点。
  - 信号柱的 CCI 数值高于正的入场阈值（默认 +50）。
- **离场条件**
  - 空头持仓：当 CCI 向上穿越 −NeutralThreshold 或向下穿越 +NeutralThreshold（默认 ±80）时平仓。
  - 多头持仓：当 CCI 向下穿越 +NeutralThreshold 或跌破 −NeutralThreshold 时平仓。
  - 策略未内置止损/止盈，可结合外部风控模块使用。

## 指标
- **Commodity Channel Index (CCI)**：确认趋势强度，默认周期 25。
- **蜡烛实体长度的简单移动平均**：计算最近 *BodyAveragePeriod* 根K线的平均实体长度（默认 5），用于验证形态强度。

## 参数
| 名称 | 说明 | 默认值 | 备注 |
| --- | --- | --- | --- |
| `CciPeriod` | CCI 指标计算周期。 | 25 | 可参与优化。 |
| `BodyAveragePeriod` | 计算平均实体长度的窗口。 | 5 | 可参与优化。 |
| `EntryThreshold` | 入场所需的 CCI 绝对值。 | 50 | 取正值，策略会比较 ±EntryThreshold。 |
| `NeutralThreshold` | 确认离场的 CCI 绝对阈值。 | 80 | 取正值，策略会比较 ±NeutralThreshold。 |
| `CandleType` | 参与计算的K线类型或周期。 | 1 小时时间框架 | 可根据需求调整。 |

## 其他说明
- 策略通过 `SubscribeCandles` 订阅K线，并利用 `Bind` 同步获取指标数值。
- 交易指令使用 `BuyMarket` 与 `SellMarket` 市价单完成。
- 代码中的注释全部为英文，符合项目要求。
- 如需额外风控，可结合 `StartProtection` 或自定义资金管理逻辑。
