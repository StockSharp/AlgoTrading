# Money Rain 策略

## 概述
- 将原始的 **MoneyRain (barabashkakvn 版本)** MQL5 专家顾问迁移到 StockSharp 高级 API。
- 通过 DeMarker 指标判断方向：数值大于 0.5 时建立多头，数值小于或等于 0.5 时建立空头。
- 任意时刻只允许持有一个仓位，止盈止损距离以“点”形式固定。

## 数据与指标
- 订阅可配置的 `CandleType`（默认 30 分钟 K 线）。
- 计算一个 `DeMarker` 指标，周期参数 `DeMarkerPeriod` 默认 31。
- 额外订阅 Level 1 报价，用于估算当前点差，以驱动动态仓位管理。

## 交易流程
1. 仅处理已经完成的 K 线，对应原脚本中的 `iTime(0)` 新柱判定。
2. 在持仓期间监控 K 线的最高/最低价与预先计算的止损、止盈价格，当任一价格被触发时使用市价单平仓，并记录结果是盈利还是亏损。
3. 当没有持仓且未达到连续亏损上限时，计算下一笔订单的数量。
4. 若 `DeMarker > 0.5` 则买入，否则卖出。提交市价单前会取消所有挂单。

## 资金管理
- 复刻 MQL 中 `getLots()` 的逻辑，维护以下状态：
  - `_lossesVolume`：最近亏损交易的累计数量，相对于基础手数进行归一化。
  - `_consecutiveLosses` 与 `_consecutiveProfits`：连亏/连盈计数器，用于决定何时重置亏损累积。
- 在连亏后出现第一笔盈利时（`_consecutiveProfits == 0`），下一单的手数按照原公式增加：
  \[
  \text{volume} = \text{BaseVolume} \times \frac{_lossesVolume \times (\text{StopLossPoints} + \text{spread})}{\text{TakeProfitPoints} - \text{spread}}
  \]
- 点差通过最优买/卖价估算（以点为单位）。若 Level 1 尚未到达，则点差视为 0。
- 将 `FastOptimize` 设置为 `true` 可关闭自适应手数，始终使用基础手数。

## 风险控制
- `StopLossPoints` 与 `TakeProfitPoints` 通过证券的最小价差转换成绝对价格；对于 3 或 5 位小数的品种按照原策略增加 10 倍系数（对应 `digits_adjust`）。
- `LossLimit` 限制连续亏损次数，超过后停止开仓（默认值 1,000,000，相当于不限制）。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `DeMarkerPeriod` | DeMarker 指标周期。 | 31 |
| `TakeProfitPoints` | 止盈距离（点）。 | 5 |
| `StopLossPoints` | 止损距离（点）。 | 20 |
| `BaseVolume` | 基础下单手数。 | 0.01 |
| `LossLimit` | 允许的最大连续亏损次数。 | 1,000,000 |
| `FastOptimize` | `true` 时禁用自适应加仓。 | `false` |
| `CandleType` | 用于计算的 K 线类型。 | 30 分钟 K 线 |

## 实现说明
- 止损/止盈通过比较当前 K 线的最高价和最低价模拟触发，若同一根 K 线同时触及两个目标，策略保守地认为先触发止损。
- 使用 `OnOwnTradeReceived` 监控平仓成交，以便更新连盈连亏计数和亏损手数累积。
- 源代码使用制表符缩进并保持英文注释，符合仓库约定。

## 目录结构
- `CS/MoneyRainStrategy.cs`：策略实现。
- `README.md` / `README_ru.md` / `README_cn.md`：多语言文档。

## 与 MQL 版本的差异
- 原先在服务器端挂出的保护性订单改为根据 K 线区间触发的市价平仓。
- 点差来自 Level 1 报价而非 MetaTrader 符号属性。
- 删除了邮件通知和 `IsTradeAllowed` 检查，相关责任由 StockSharp 运行环境承担。
