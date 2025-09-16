# Ema612CrossoverStrategy

## 概述
- 将 MetaTrader 5 智能交易程序 **"EMA 6.12 (barabashkakvn's edition)"** 移植到 StockSharp 的高级 API。
- 使用一条快速和一条慢速的简单移动平均线（原脚本同样基于 MODE_SMA）交叉作为交易信号。
- 通过绝对价格单位定义的可选止盈与移动止损，使策略可以针对不同交易品种灵活调参。

## 交易逻辑
### 数据准备
- 策略订阅由 `CandleType` 指定的 K 线（默认 15 分钟）。
- 计算两条 SMA：`FastPeriod` 为快速曲线的长度，`SlowPeriod` 为慢速曲线的长度，并要求慢速周期大于快速周期。

### 入场条件
- 仅在每根 K 线收盘后评估信号。
- 当上一根 K 线上慢速 SMA 在快速 SMA 之上，而当前 K 线慢速 SMA 下穿快速 SMA 时判定为**看多交叉**。若持有空头则先平仓，再按 `Volume` 开多头。
- 当上一根 K 线上慢速 SMA 在快速 SMA 之下，而当前 K 线慢速 SMA 上穿快速 SMA 时判定为**看空交叉**。若持有多头则先平仓，再按 `Volume` 开空头。

### 出场条件
- 出现相反交叉信号时立即平掉当前仓位。
- 若 `TakeProfitOffset` 大于 0，则在入场价格基础上计算固定止盈：多头目标价 `entry + TakeProfitOffset`，空头目标价 `entry - TakeProfitOffset`。
- 若 `TrailingStopOffset` 大于 0，则启用移动止损。只有当浮动利润超过 `TrailingStopOffset + TrailingStepOffset` 后才开始上移/下移止损，使其与最新收盘价保持 `TrailingStopOffset` 的距离，并且新的止损价必须至少向盈利方向移动 `TrailingStepOffset`。多头使用最低价触发止损，空头使用最高价触发。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | 15 分钟时间框架 | 计算均线与生成信号所使用的 K 线类型。 |
| `FastPeriod` | 6 | 快速 SMA 的周期，必须大于 0 且小于 `SlowPeriod`。 |
| `SlowPeriod` | 54 | 慢速 SMA 的周期，必须大于 0 且大于 `FastPeriod`。 |
| `Volume` | 1 | 新开仓时提交的交易量。 |
| `TakeProfitOffset` | 0.001 | 以绝对价格单位定义的止盈距离，设置为 0 可关闭。 |
| `TrailingStopOffset` | 0.005 | 移动止损与价格之间的绝对距离，设置为 0 可关闭。 |
| `TrailingStepOffset` | 0.0005 | 每次调整移动止损所需的额外盈利幅度。 |

> **提示：** 所有距离参数均使用绝对价格单位。请根据交易品种的最小报价单位进行换算，例如 EURUSD 的最小报价增量为 0.0001，则默认值约等于 10、50 和 5 个点。

## 实现细节
- 使用项目规范要求的 `SubscribeCandles().Bind()` 高级管线搭建指标和回调。
- 若环境支持图表，将绘制两条 SMA 并标记交易。
- 使用字段保存入场价、当前移动止损和止盈，重现 MQL5 版本的状态管理。
- 在启动时强制检查 `SlowPeriod > FastPeriod`，避免错误的指标设置。

## 使用建议
- 根据市场特性优化时间框架与 SMA 周期（短周期适合日内，长周期适合波段）。
- 在运行前把点数或跳动值换算成绝对价格，以正确配置止盈和移动止损。
- 把 `TrailingStopOffset` 设为 0 可以关闭移动止损，此时仅依靠反向交叉或可选止盈离场。
