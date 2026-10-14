# PowerHouse SwiftEdge AI v2.10 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

基于枢轴的突破策略并结合动量过滤和简单风险控制。
当价格突破最近枢轴高点且动量充足并满足最小信号间隔时开多。
当价格跌破枢轴低点且动量为负时开空。
仓位按照设定的止盈和止损点数退出。

## 计算与执行

这是确定性示例；历史名称不代表训练好的 AI 模型。C# 和 Python 共同使用下列对上述简介的明确解释。

使用已完成的基础K线。枢轴高点必须严格高于两侧各 `PivotPeriod` 根相邻K线的最高价，枢轴低点必须严格低于它们的最低价；同价不形成枢轴。右侧K线全部完成后才确认，不回填历史信号。分别保存最近确认的高点和低点。

做多要求 `前一 Close <= 枢轴高点` 且 `当前 Close > 枢轴高点`；做空要求反向穿越枢轴低点。动量为 `当前 Close - MomentumPeriod 根前的 Close`，做多必须严格大于 `MomentumThreshold`，做空必须严格小于其负值。双向信号都要求距上次接受的入场至少 `MinSignalBars` 根已完成K线；零禁用间隔。被拒绝的信号不延迟执行，没有 EMA 过滤或六小时冷却期。

空仓以 `Volume` 入场，反向仓位以 `Volume + abs(Position)` 完全反转。有未完成订单时禁止新订单。止损和止盈是固定报价价格单位：一点表示价格差 1，不是 tick、`PriceStep` 倍数或百分比，需按品种调整。基准为实际成交价；同方向部分加仓用成交量加权均价，减仓保留基准，反转重新建立基准。

本地保护在多仓当前 bid 或空仓当前 ask 达到止损或止盈时，用市价单平掉全部仓位。已完成K线收盘价也检查保护；不会把最高/最低价触及当作有顺序的盘中事件。保护不受入场间隔限制，同一K线上优先于新入场。价格跳空时成交价可能不同于触发价。

默认：`PivotPeriod=2`、`MomentumPeriod=10`、`MomentumThreshold=0`、`MinSignalBars=5`、`StopLossPoints=50`、`TakeProfitPoints=100`、`CandleType=TimeSpan.FromMinutes(5).TimeFrame()`。周期和两个退出距离必须为正；动量阈值和间隔不能为负。
