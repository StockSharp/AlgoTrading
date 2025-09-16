# RSI Sign 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MQL5 的 **iRSISign** 智能交易系统转换为 StockSharp 的高级 API。它结合 RSI 和 ATR 指标来产生交易信号。

系统只处理选定时间框架的已完成K线。当 RSI 从下方穿越 `DownLevel` 时，策略开多或平空；当 RSI 从上方跌破 `UpLevel` 时，策略开空或平多。ATR 仅用于提供背景信息，类似原始指标使用 ATR 偏移显示信号箭头。

## 细节

- **入场条件**：
  - **做多**：先前 RSI 低于 `DownLevel`，当前 RSI 上穿该水平。
  - **做空**：先前 RSI 高于 `UpLevel`，当前 RSI 跌破该水平。
- **方向**：支持多空，并可分别启用。
- **出场条件**：
  - 相反信号在对应的关闭标志开启时平仓。
- **止损**：未实现，如有需要可外部添加。
- **默认参数**：
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `UpLevel` = 70
  - `DownLevel` = 30
  - `CandleType` = 1小时K线
- **过滤器**：
  - 类别：动量
  - 方向：双向
  - 指标：RSI、ATR
  - 止损：无
  - 复杂度：基础
  - 时间框架：可变
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

## 参数

| 名称 | 说明 |
|------|------|
| `RsiPeriod` | RSI 周期 |
| `AtrPeriod` | ATR 周期 |
| `UpLevel` | RSI 上限阈值，触发卖出信号 |
| `DownLevel` | RSI 下限阈值，触发买入信号 |
| `CandleType` | 计算所用的K线时间框架 |
| `BuyOpen` | 允许开多 |
| `SellOpen` | 允许开空 |
| `BuyClose` | 在反向信号时允许平多 |
| `SellClose` | 在反向信号时允许平空 |

该策略主要用于演示如何将简单的 MQL5 逻辑迁移到 StockSharp 策略框架。
