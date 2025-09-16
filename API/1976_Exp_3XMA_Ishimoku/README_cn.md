# Exp 3XMA Ichimoku 策略

该策略是 MQL 专家 `exp_3xma_ishimoku` 的转换版本。策略使用缩短周期的 Ichimoku 指标，并逆向处理云层突破。

当 Kijun 线从云层上方跌入云内时，策略平掉空头并在允许的情况下开多。当 Kijun 线从云层下方升入云内时，策略平掉多头并可能开空。

默认使用 4 小时蜡烛图。

## 参数
- **Tenkan Period** – Tenkan-sen 周期。
- **Kijun Period** – Kijun-sen 周期。
- **Senkou Span B Period** – 第二条 Senkou 线的周期。
- **Allow Buy** – 是否允许开多。
- **Allow Sell** – 是否允许开空。
- **Candle Type** – 用于计算指标的蜡烛类型。

## 工作原理
1. 订阅所选蜡烛并绑定 Ichimoku 指标。
2. 仅处理已完成的蜡烛。
3. 检测 Kijun 与云层边界的交叉。
4. 在允许的情况下关闭相反头寸并开立新的信号方向头寸。

## 免责声明
本示例仅供学习，不构成投资建议，风险自担。
