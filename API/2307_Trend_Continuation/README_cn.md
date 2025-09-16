# 趋势延续策略

该策略利用两条指数移动平均线来识别趋势的延续。当快 EMA 上穿慢 EMA 时开多单；当快 EMA 下穿慢 EMA 时开空单。

## 参数
- **Fast EMA Length**：快线 EMA 的周期（默认 20）。
- **Candle Type**：使用的K线周期（默认 4 小时）。
- **Stop Loss**：保护性止损，在启动时通过 `StartProtection` 启用（默认 1000）。
- **Take Profit**：盈利目标，通过 `StartProtection` 启用（默认 2000）。

## 工作原理
1. 启动时订阅指定的K线并创建两条 EMA 指标。
2. 每当收到完成的K线，就检测快慢 EMA 的交叉。
3. 快线从下向上穿越慢线时开多并关闭空单；相反的交叉则开空并关闭多单。
4. 通过止损和止盈参数控制风险。

该示例是对原始 MQL `Exp_TrendContinuation` 专家的简化转换。
