# Nova Futures PRO SAFE v6 策略

该策略结合趋势、波动和结构信号。它使用200周期EMA与ADX确认趋势，利用布林带与肯特纳通道的挤压释放捕捉突破，并通过唐奇安通道的前高前低判断结构。可选的高周期过滤器和Choppiness指数帮助避开震荡市场，冷却期避免刚平仓就重新入场。

## 输入参数
- **EMA Length** — 基础指数均线长度
- **DMI Length** — ADX及方向性指标周期
- **Min ADX** — 认为存在趋势的最小ADX
- **BB Length** — 布林带周期
- **BB Mult** — 布林带倍数
- **KC Length** — 肯特纳通道周期
- **KC Mult** — 肯特纳通道倍数
- **Donchian Length** — 结构通道回溯期
- **Use HTF** — 启用高周期确认
- **HTF Candle** — 高周期时间框架
- **HTF EMA** — 高周期EMA长度
- **HTF Min ADX** — 高周期最小ADX
- **Use Choppiness** — 启用Choppiness过滤
- **Chop Length** — Choppiness周期
- **Chop Threshold** — 最大允许Choppiness
- **Cooldown** — 平仓后等待的K线数量
- **Candle Type** — 主时间框架

## 说明
本策略为 TradingView 脚本“Nova Futures PRO (SAFE v6) — HTF + Choppiness + Cooldown”的简化移植版。
