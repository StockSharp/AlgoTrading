# Color JFATL Digit Duplex 策略

## 概述
Color JFATL Digit Duplex 策略源自 MetaTrader 5 专家顾问 `Exp_ColorJFatl_Digit_Duplex`，在 StockSharp 高级 API 中实现为双模块系统。策略同时运行多头与空头两套信号流，全部基于 Color Jurik Fast Adaptive Trend Line（JFATL）指标。多头模块在颜色转为多头（值为 2）时尝试开仓，空头模块在颜色转为空头（值为 0）时入场。每个模块都拥有独立的平滑、价格源、数字化精度、信号柱偏移以及止损/止盈距离设置。

本转换版本实现了原始 FATL 核心权重与 Jurik 平滑算法，并将结果数字化，以便与 MetaTrader 指标保持一致。指标向策略处理器返回目标柱的颜色编码以及前一柱的颜色，从而能够完全复现原策略的触发条件。

## 指标逻辑
1. **FATL 卷积**：根据所选价格类型获取最近 39 根数据，并使用原始 FATL 权重计算滤波值。
2. **Jurik 平滑**：将 FATL 输出送入 Jurik Moving Average。由于 StockSharp 版本没有公开的相位属性，本实现通过差分调整模拟 Phase 参数带来的超前/滞后效果。
3. **数字化处理**：按照设定的位数对平滑结果进行四舍五入，生成与原指标一致的“Digit”输出。
4. **颜色判定**：若当前值高于上一值，颜色置为 2；低于上一值置为 0；否则沿用上一颜色。`SignalBar` 参数决定向前回看几根已完成的柱，并同时获取更早一根的颜色值。

指标以复合值形式返回：包括数字化后的 JFATL、当前颜色、前一颜色以及信号柱收盘时间。策略逻辑据此判断颜色变化并生成交易信号。

## 交易规则
- **多头模块**
  - 当 `SignalBar` 对应的颜色由非 2 变为 2 且当前无多头持仓时开多。
  - 当 `SignalBar` 颜色变为 0 时平掉现有多头。
- **空头模块**
  - 当 `SignalBar` 颜色由大于 0 变为 0 且当前无空头持仓时开空。
  - 当 `SignalBar` 颜色变为 2 时平掉现有空头。
- **仓位管理**：开仓时会先使用市场单抵消相反方向的持仓，确保任意时刻仅保持一个净仓位。平仓使用 `ClosePosition()`，避免在帐户中同时存在多笔订单。

## 风险控制
多头与空头模块分别设定以价格最小变动单位计的止损与止盈距离。开仓后记录入场价并依据 `PriceStep` 计算绝对价格目标。在每次指标更新（即订阅的蜡烛收盘）时检查当前蜡烛的高低点：

- 多头：若最低价触及止损价或最高价触及止盈价，则立即平仓。
- 空头：若最高价触及止损价或最低价触及止盈价，则立即平仓。

当距离设为 0 时，对应保护措施关闭，仅依靠指标反向信号退出。

## 参数说明
| 分组 | 参数 | 描述 |
| --- | --- | --- |
| 通用 | `LongCandleType` | 多头指标使用的蜡烛类型（时间框架）。 |
| 通用 | `ShortCandleType` | 空头指标使用的蜡烛类型。 |
| 指标（多头） | `LongJmaLength` | 多头 Jurik 移动平均周期。 |
| 指标（多头） | `LongJmaPhase` | 多头 Jurik 相位调整（−100 至 100）。 |
| 指标（多头） | `LongAppliedPrice` | 参与 FATL 卷积的价格源。 |
| 指标（多头） | `LongDigit` | 数字化位数。 |
| 指标（多头） | `LongSignalBar` | 信号柱偏移，0 表示最新收盘柱。 |
| 风险（多头） | `LongStopLossPoints` | 多头止损距离（以 price step 表示）。 |
| 风险（多头） | `LongTakeProfitPoints` | 多头止盈距离。 |
| 交易（多头） | `EnableLongOpen` | 是否允许新的多头入场。 |
| 交易（多头） | `EnableLongClose` | 是否允许根据指标信号平多。 |
| 指标（空头） | `ShortJmaLength` | 空头 Jurik 移动平均周期。 |
| 指标（空头） | `ShortJmaPhase` | 空头 Jurik 相位调整。 |
| 指标（空头） | `ShortAppliedPrice` | 空头模块使用的价格源。 |
| 指标（空头） | `ShortDigit` | 空头数字化位数。 |
| 指标（空头） | `ShortSignalBar` | 空头信号柱偏移。 |
| 风险（空头） | `ShortStopLossPoints` | 空头止损距离。 |
| 风险（空头） | `ShortTakeProfitPoints` | 空头止盈距离。 |
| 交易（空头） | `EnableShortOpen` | 是否允许新的空头入场。 |
| 交易（空头） | `EnableShortClose` | 是否允许根据指标信号平空。 |

## 使用提示
1. 根据需求分别设定多头与空头的蜡烛类型，可使用不同时间框架。
2. 调整价格源与数字化位数以贴合目标品种，与原 MT5 设置保持一致。
3. `SignalBar` 控制回看几根已收盘柱。默认值 1 对应原专家顾问的上一根完成柱。
4. 请确保策略 `Volume` 属性设置为希望的下单量。策略在翻仓时会自动加上当前仓位的绝对值，以实现一笔反向单即可完成反手。
5. 止损止盈依赖 `PriceStep`。若品种未提供该信息，距离将直接按数值解释。

## 转换说明
- 由于 StockSharp 的 JurikMovingAverage 没有显式 Phase 属性，本实现通过对平滑输出的差分调整模拟相位效果，从而保持原策略在快速或滞后响应方面的特点。
- 原 MT5 策略可能同时持有多笔订单。本转换采用单一净仓位模型，所有交易都体现在 `Strategy.Position` 上。
- 止损止盈检测在指标蜡烛收盘时进行，与原策略依赖已完成柱的信号频率相符，并满足高阶 API 避免逐笔行情处理的要求。

## 文件列表
- `CS/ColorJfatlDigitDuplexStrategy.cs`：策略与自定义指标实现。
- `README.md` / `README_cn.md` / `README_ru.md`：英文、中文、俄文说明文档。
