# RSI CCI Williams %R
[English](README.md) | [Русский](README_ru.md)

该策略结合 RSI、CCI 和 Williams %R 用于捕捉可能的反转。当三个指标同时处于超卖区时买入，处于超买区时卖出。每笔交易都使用基于百分比的止盈和止损。

## 细节

- **入场条件**：
  - **做多**：`RSI < RSI 超卖` && `CCI < CCI 超卖` && `Williams %R < Williams 超卖`
  - **做空**：`RSI > RSI 超买` && `CCI > CCI 超买` && `Williams %R > Williams 超买`
- **离场条件**：通过止盈或止损退出。
- **类型**：反转
- **指标**：RSI、CCI、Williams %R
- **时间框架**：45 分钟（默认）
- **止损**：百分比止盈和止损
