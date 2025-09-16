# 量价加权MA数字系统策略

该策略实现 **Volume Weighted MA Digit System**。它基于K线的最高价和最低价构建两条成交量加权移动平均线（VWMA）。价格穿越这些通道时生成交易信号。

## 工作原理

1. **指标**
   - `VWMA High`：基于最高价的VWMA。
   - `VWMA Low`：基于最低价的VWMA。
2. **信号**
   - **做多**：收盘价向上穿越 `VWMA High`。
   - **做空**：收盘价向下穿越 `VWMA Low`。
   - 反向穿越用于平掉已有仓位。
3. **风险控制**
   - 使用 `StartProtection` 设置的止损和止盈（点数）。

## 参数

| 名称 | 说明 | 默认值 |
|------|------|-------|
| `VwmaPeriod` | VWMA 计算周期 | `12` |
| `CandleType` | 使用的K线周期 | `4h` |
| `StopLoss` | 止损点数 | `1000` |
| `TakeProfit` | 止盈点数 | `2000` |

## 备注

- 仅处理已完成的K线。
- 策略采用高层API（`SubscribeCandles`、`Bind`）以及标准指标。
- 原始 MQL 策略：`Exp_Volume_Weighted_MA_Digit_System.mq5`。
