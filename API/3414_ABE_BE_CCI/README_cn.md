# ABE BE CCI 吞没策略
[English](README.md) | [Русский](README_ru.md)

本策略将 MetaTrader 5 专家顾问 **Expert_ABE_BE_CCI**（目录 `MQL/306`）移植到 StockSharp。原始 EA 结合了看涨/看跌吞没形态与 Commodity Channel Index (CCI) 指标，并使用固定手数管理。C# 版本保留相同的判定规则，同时利用 StockSharp 的高层订阅与指标绑定功能，使实现更加清晰。

系统仅处理选定周期的已完成 K 线。每根新 K 线都会计算最近 `BodyAveragePeriod` 根的实体均值、收盘价均值以及周期为 `CciPeriod` 的 CCI。当满足以下条件时才认定为有效吞没：当前实体超过均值、收盘突破前一根的开盘、被吞没蜡烛的中点位于均值的正确一侧——这些条件与 MQL 中 `CCandlePattern` 的检查一致。多头需看涨吞没且 CCI 低于 `EntryOversoldLevel`，空头需看跌吞没且 CCI 高于 `EntryOverboughtLevel`。退出规则复制 EA 的 40 分“投票”：CCI 穿越 ±`ExitLevel` 会立即平掉当前持仓。

## 执行流程

1. 订阅 `CandleType` 指定的蜡烛，并同时计算：
   - `BodyAveragePeriod` 长度的实体平均值；
   - 相同窗口的收盘价均值；
   - 周期为 `CciPeriod` 的 CCI。
2. 每当一根新蜡烛收盘：
   - 检查上一根蜡烛颜色相反且被当前实体完全包覆；
   - 验证实体大于均值且收盘越过上一根开盘；
   - 通过上一根蜡烛中点与均线的位置关系判断趋势背景；
   - 使用 CCI 与相应阈值确认动量。
3. 交易处理：
   - 条件满足且当前无多单时，先平空再按 `Volume` 开多；
   - 条件满足且当前无空单时，先平多再按 `Volume` 开空；
   - CCI 穿越 `+ExitLevel` 或跌破 `-ExitLevel` 时平多，CCI 自下向上穿越 `-ExitLevel` 或跌破 `+ExitLevel` 时平空。

## 默认参数

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `CciPeriod` | 49 | CCI 指标长度。 |
| `BodyAveragePeriod` | 11 | 计算实体均值与收盘均值的窗口。 |
| `EntryOversoldLevel` | -50 | 看涨吞没的 CCI 确认阈值。 |
| `EntryOverboughtLevel` | 50 | 看跌吞没的 CCI 确认阈值。 |
| `ExitLevel` | 80 | CCI 触发离场的绝对值。 |
| `CandleType` | 1 小时 | 订阅的蜡烛类型。 |

## 备注

- 下单数量沿用原策略：`Volume` 表示基础手数，方向反转时先关闭已有仓位。
- MQL 中的 `TrailingNone` 和 `MoneyFixedLot` 未单独移植，StockSharp 已提供等价的下单行为。
- 源码中的注释全部使用英文，缩进为制表符，指标值通过 `Bind` 获取，无需调用 `GetValue`，符合仓库要求。
