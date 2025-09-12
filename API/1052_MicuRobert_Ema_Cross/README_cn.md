# MicuRobert EMA Cross Strategy
[English](README.md) | [Русский](README_ru.md)

该策略使用两条零延迟指数移动平均线（ZLEMA）进行交叉交易，可选交易时段过滤和追踪止损。

## 细节

- **做多条件：** 快速 ZLEMA 上穿慢速 ZLEMA，或价格上穿快速 ZLEMA 且快速线高于慢速线。
- **做空条件：** 快速 ZLEMA 下穿慢速 ZLEMA，或价格下穿快速 ZLEMA 且快速线低于慢速线。
- **退出：** 追踪止损或固定止损/止盈。
- **止损：** 可选追踪止损与固定止损止盈。
- **过滤器：** 交易时段过滤。
