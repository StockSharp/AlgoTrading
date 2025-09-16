# EMA Cross 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于两条指数移动平均线 (EMA) 的交叉。
当快速 EMA 上穿慢速 EMA 时开多；当快速 EMA 下穿慢速 EMA 时开空。
**Reverse** 参数可交换两条 EMA 的角色，从而反转信号方向。

每笔仓位都设置固定的 **Take Profit** 和 **Stop Loss**。
可选的 **Trailing Stop** 在价格向有利方向移动后跟随价格，锁定盈利。

策略仅处理已完成的K线，并使用高级 API 绑定指标和订阅K线数据。

## 参数
- K线类型
- 快速 EMA 长度
- 慢速 EMA 长度
- Take profit
- Stop loss
- Trailing stop
- Reverse
