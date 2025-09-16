# BykovTrend + ColorX2MA MMRec 策略
[English](README.md) | [Русский](README_ru.md)

本策略在 StockSharp 平台上重现 MQL5 专家 `Exp_BykovTrend_ColorX2MA_MMRec` 的逻辑。它包含两个独立模块：
BykovTrend 使用 Williams %R 为K线着色，而 ColorX2MA 通过双重平滑均线判断斜率。只要选定模块检测到新的颜色/
斜率变化就会尝试进场，资金管理部分简化为直接使用 `Strategy.Volume`，并可通过 StockSharp 的保护模块启用
百分比止损/止盈。

## 策略逻辑

### BykovTrend 模块
- 基于 `BykovTrendCandleType`（默认2小时K线）计算 Williams %R（长度 `BykovTrendWprLength`）。
- `BykovTrendRisk` 控制多空阈值（`33 - Risk` 和 `-Risk`）。
- 在 `BykovTrendSignalBar` 指定的历史K线上检查颜色。
- 颜色 < 2 时视为看多：若启用 `AllowBykovTrendCloseSell` 会平掉空单，若 `EnableBykovTrendBuy` 为真且上一根颜色
  不是多头则开多。
- 颜色 > 2 时视为看空：若启用 `AllowBykovTrendCloseBuy` 会平掉多单，若 `EnableBykovTrendSell` 为真且上一根颜色
  不是空头则开空。

### ColorX2MA 模块
- 连续应用两段平滑（`ColorX2MaMethod1`/`ColorX2MaLength1` 与 `ColorX2MaMethod2`/`ColorX2MaLength2`）到
  `ColorX2MaPriceType` 指定的价格，使用 `ColorX2MaCandleType` 的K线数据。
- 第二段输出与前一值比较，得到上升(1)、下降(2)或平稳(0)三种斜率状态。
- 在 `ColorX2MaSignalBar` 指定的历史K线上评估斜率状态。
- 斜率=1 时若启用 `AllowColorX2MaCloseSell` 则平空，若 `EnableColorX2MaBuy` 为真且前一状态不是 1 则开多。
- 斜率=2 时若启用 `AllowColorX2MaCloseBuy` 则平多，若 `EnableColorX2MaSell` 为真且前一状态不是 2 则开空。

### 交易管理
- 先执行平仓信号再处理开仓，以贴近原始专家的下单顺序。
- 委托数量取自 `Strategy.Volume`，未移植 MQL 中复杂的递增手数算法。
- `StopLossPercent` 与 `TakeProfitPercent` 大于 0 时会调用 `StartProtection`，启用百分比止损/止盈。

## 细节

- **方向**：支持多头与空头。
- **进场条件**：
  - BykovTrend 出现新的多头颜色。
  - ColorX2MA 出现新的上升斜率。
- **离场条件**：
  - 对应模块出现反向颜色/斜率。
  - 可选的百分比止损/止盈触发。
- **过滤器**：无额外过滤。
- **仓位规模**：固定，由 `Strategy.Volume` 决定。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `EnableBykovTrendBuy` | 允许 BykovTrend 开多。 | `true` |
| `EnableBykovTrendSell` | 允许 BykovTrend 开空。 | `true` |
| `AllowBykovTrendCloseBuy` | BykovTrend 变空时平多。 | `true` |
| `AllowBykovTrendCloseSell` | BykovTrend 变多时平空。 | `true` |
| `BykovTrendRisk` | Williams %R 阈值灵敏度。 | `3` |
| `BykovTrendWprLength` | Williams %R 周期。 | `9` |
| `BykovTrendSignalBar` | 检查颜色的历史K线索引。 | `1` |
| `BykovTrendCandleType` | BykovTrend 使用的K线类型。 | `2h` |
| `EnableColorX2MaBuy` | 允许 ColorX2MA 开多。 | `true` |
| `EnableColorX2MaSell` | 允许 ColorX2MA 开空。 | `true` |
| `AllowColorX2MaCloseBuy` | 斜率转空时平多。 | `true` |
| `AllowColorX2MaCloseSell` | 斜率转多时平空。 | `true` |
| `ColorX2MaMethod1` | 第一段平滑类型。 | `Simple` |
| `ColorX2MaLength1` | 第一段平滑周期。 | `12` |
| `ColorX2MaPhase1` | 相位占位参数（未使用）。 | `15` |
| `ColorX2MaMethod2` | 第二段平滑类型。 | `Jurik` |
| `ColorX2MaLength2` | 第二段平滑周期。 | `5` |
| `ColorX2MaPhase2` | 相位占位参数（未使用）。 | `15` |
| `ColorX2MaPriceType` | ColorX2MA 取价方式。 | `Close` |
| `ColorX2MaSignalBar` | 检查斜率的历史K线索引。 | `1` |
| `ColorX2MaCandleType` | ColorX2MA 使用的K线类型。 | `2h` |
| `StopLossPercent` | 百分比止损，0 表示关闭。 | `0` |
| `TakeProfitPercent` | 百分比止盈，0 表示关闭。 | `0` |

## 说明

- `ColorX2MaPhase1` 与 `ColorX2MaPhase2` 仅为兼容原参数而保留，StockSharp 的均线实现没有相位选项。
- 仅提供 StockSharp 中可用的平滑方法，`SmoothAlgorithms.mqh` 中无法对应的类型会退回到最接近的替代方案。
- 未移植 `TradeAlgorithms.mqh` 中的资金管理加仓算法，如需复杂仓位控制需自行扩展或外部管理。

## 使用步骤

1. 选择交易标的并设置 `Strategy.Volume` 为期望手数。
2. 如需不同周期，可调整 `BykovTrendCandleType` 与 `ColorX2MaCandleType`。
3. 根据需要调整平滑方法、周期及信号偏移。
4. 若要启用保护机制，请将 `StopLossPercent` 和/或 `TakeProfitPercent` 设为大于 0 的值。
5. 启动策略，系统会订阅所需K线，监控两个模块并按上述顺序发送市价单。
