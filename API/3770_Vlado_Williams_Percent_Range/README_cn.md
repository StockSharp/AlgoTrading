# Vlado Williams %R 阈值策略

## 概述
**Vlado Williams %R 阈值策略** 完全复刻了 MetaTrader 4 专家顾问 `Vlado_www_forex-instruments_info.mq4`。原始脚本依据 Williams %R 指标与设定阈值的关系来决定持仓方向，只允许同时持有一个头寸。本移植版本沿用了同样的判定流程，并将所有关键输入封装为 StockSharp 参数，便于优化与界面配置。

### 核心特点
- 仅使用一个 Williams %R 指标：当数值高于阈值时做多，低于阈值时做空。
- 每次只有一个方向的仓位，先平仓再考虑反向建仓，避免同一根 K 线内的即刻反转。
- 可选的资金管理逻辑，按照 `账户权益 × MaximumRiskPercent ÷ 100 ÷ 收盘价` 估算下单数量，近似原始的 `AccountFreeMargin * MaximumRisk / price` 公式。
- `CandleType` 参数决定信号所用的时间框架，默认 15 分钟 K 线，可根据交易品种自由调整。

## 交易流程
1. 订阅 `CandleType` 指定的蜡烛序列，并计算长度为 `WprLength`（默认 100）的 Williams %R。
2. 若 Williams %R **高于** `WprLevel`：
   - 标记为多头偏好。如果当前无仓位且上一笔交易并非多单，则发送市价买单。
   - 若当前持有空单，会立即平仓；新的多单会在下一根完成的 K 线上评估。
3. 若 Williams %R **低于** `WprLevel`：
   - 标记为空头偏好。如果当前无仓位且上一笔交易并非空单，则发送市价卖单。
   - 若当前持有多单，会立即平仓。
4. `CalculateOrderVolume` 负责最终数量：
   - 当 `UseRiskMoneyManagement` 为 **true** 时，使用最新收盘价估算风险仓位；否则回退到策略的 `Volume` 属性。
   - 数量会对齐到 `Security.VolumeStep`，并检查 `MinVolume` / `MaxVolume` 限制，防止触发交易所拒单。

与原 EA 一样，策略在平仓的同一根 K 线上不会立刻开反向仓，确保资金管理逻辑有时间刷新。

## 移植说明
- `MaximumRiskPercent` 默认值为 `10`，对应源代码 `MaximumRisk = 10` 的初始设定，能在典型外汇账户上产生类似的手数。
- 原脚本中的 `shift` 参数始终为 0，因此未加入新的策略。
- MetaTrader 中用于着色的常量（`Red`、`Blue` 等）在 StockSharp 中没有直接用途，已经移除。
- 滑点设置由撮合系统自行处理，不再需要额外的 `slippage` 参数。

## 参数
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CandleType` | `DataType` | 15 分钟 | 信号与下单使用的时间框架。 |
| `WprLength` | `int` | 100 | Williams %R 指标的回溯长度。 |
| `WprLevel` | `decimal` | `-50` | 划分多空区域的阈值。 |
| `UseRiskMoneyManagement` | `bool` | `false` | 是否启用基于权益的仓位控制。 |
| `MaximumRiskPercent` | `decimal` | `10` | 启用资金管理时，每笔交易投入的权益百分比。 |

> **提示：** 策略本身不包含止损或止盈，请结合 `StartProtection()`、外部风控或交易所风控参数一同使用。

## 使用建议
1. 绑定具有准确 `PriceStep`、`StepPrice`、`VolumeStep`、`MinVolume`/`MaxVolume` 的标的，确保仓位换算与对齐正确。
2. 将 `Volume` 设置为手动控制的基准手数。在无法获取账户权益或关闭资金管理时，会直接使用该值。
3. 通过优化器调整 `WprLevel` 与 `WprLength`，以匹配不同的市场节奏。阈值靠近 `-50` 会频繁持仓，阈值靠近 `-20/-80` 则更加保守。
4. 策略属于趋势跟随型，在震荡行情中可能频繁反复开平。可叠加其他过滤器，例如较长周期趋势、波动率或成交量条件。

## 与 MT4 版本的差异
- 采用 StockSharp 高级 API 的订阅与绑定机制，不再手动遍历订单或历史记录。
- 仓位控制依赖 `Portfolio.CurrentValue`，若账户权益不可用则回退到固定手数，与原策略 `mm = 0` 的行为一致。
- 所有注释与描述均使用英文，符合仓库统一要求。

## 检查清单
- ✅ 代码遵循策略模板规范：文件作用域命名空间、制表符缩进、`inheritdoc` 注释等。
- ✅ 参数通过 `Param()` 创建，并在需要时启用 `SetCanOptimize(true)` 以便优化。
- ✅ Williams %R 数值通过 `Bind` 获取，没有调用被禁止的 `GetValue()` 接口。
