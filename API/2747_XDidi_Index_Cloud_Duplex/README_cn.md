# XDidi Index Cloud Duplex 策略

## 概述
XDidi Index Cloud Duplex 策略将 MQL5 专家顾问 *Exp_XDidi_Index_Cloud_Duplex* 的双通道逻辑迁移到 StockSharp。策略同时维护多头与空头两套 XDidi 指标配置，通过可配置的时间框架计算快速、慢速均线与中轴均线的比例关系，并据此发出入场与出场信号。

## 交易逻辑
1. **指标计算**
   - 对每个模块分别计算三条均线（快线、中线、慢线），价格来源可选收盘价、典型价、Demark 价等。
   - XDidi 指数定义为 `fast / medium` 与 `slow / medium`。如果启用 `Reverse`，两个比值同时取相反数。
2. **信号判定**
   - 多头模块：若前一根参考柱存在 `fast > slow` 且信号柱收盘时 `fast <= slow`，触发做多；若前一柱 `fast < slow`，触发平多。
   - 空头模块：若前一根参考柱 `fast < slow` 且信号柱 `fast >= slow`，触发做空；若前一柱 `fast > slow`，触发平空。
   - `LongSignalBar` 与 `ShortSignalBar` 控制信号柱与上一参考柱的偏移量。
3. **订单执行**
   - 使用策略的 `Volume` 下市场单，若已有相反头寸会先行平仓再反向开仓。
   - `StopLossPoints` 与 `TakeProfitPoints` 以价格步长表示，最终通过 `StartProtection` 应用。

## 参数说明
- **时间框架**：`LongCandleType`、`ShortCandleType`。
- **均线类型与周期**：`LongFast/Medium/SlowMethod`、`ShortFast/Medium/SlowMethod` 及对应长度。缺少的平滑算法（JJMA、JurX、ParMA、VIDYA）退化为指数均线。
- **价格来源**：`LongAppliedPrice`、`ShortAppliedPrice`。
- **交易开关**：`EnableLongEntries`、`EnableLongExits`、`EnableShortEntries`、`EnableShortExits`。
- **信号柱偏移**：`LongSignalBar`、`ShortSignalBar`。
- **比值取反**：`LongReverse`、`ShortReverse`。
- **风险控制**：`StopLossPoints`、`TakeProfitPoints`（为 0 时关闭）。
- **下单数量**：基础属性 `Volume`。

## 实现细节
- 使用 StockSharp 自带的均线指标；无法一一对应的平滑算法采用指数均线作为替代。
- 仅在蜡烛收盘后处理信号，与原版 `IsNewBar` 行为一致。
- 仅缓存所需的少量历史值，避免维护大型集合。
- 即便停用止损/止盈，仍调用 `StartProtection()` 以保持策略生命周期一致。

## 使用建议
- 确认数据源能够提供所选时间框架的蜡烛。
- 根据品种特性调整均线参数与价格类型，可利用优化器批量测试。
- 当多空模块使用不同时间框架时，图表会为每个模块创建独立区域，便于观察。

## 与 MQL5 版本的差异
- 未实现原始 EA 中的资金管理模式（MM、MarginMode），下单量由 `Volume` 控制。
- `SmoothAlgorithms.mqh` 中的部分特殊平滑算法使用近似实现。
- 止损/止盈不再单独附加到订单，而是通过策略保护统一处理。
