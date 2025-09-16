# ColorJFatl StDev 策略

该策略将 MQL5 中的 **ColorJFatl_StDev** 专家顾问移植到 StockSharp API。它结合 Jurik 移动平均线 (JMA) 与标准差通道来生成交易信号。

## 策略逻辑

1. 对收盘价计算 JMA。
2. 按设定周期计算标准差。
3. 使用系数 `K1` 和 `K2` 构建两组动态通道：
   - `upper1 = JMA + K1 * StdDev`
   - `upper2 = JMA + K2 * StdDev`
   - `lower1 = JMA - K1 * StdDev`
   - `lower2 = JMA - K2 * StdDev`
4. 根据所选信号模式开平仓：
   - **Point**：价格穿越通道时触发。
   - **Direct**：基于 JMA 曲线的转折点。
   - **Without**：禁用相应信号。

## 参数

| 名称 | 说明 |
|------|------|
| `CandleTimeFrame` | K线时间框架 |
| `JmaLength` | JMA 周期 |
| `JmaPhase` | JMA 相位 |
| `StdPeriod` | 标准差周期 |
| `K1` | 第一倍数 |
| `K2` | 第二倍数 |
| `BuyOpenMode` | 多头开仓模式 |
| `SellOpenMode` | 空头开仓模式 |
| `BuyCloseMode` | 多头平仓模式 |
| `SellCloseMode` | 空头平仓模式 |

## 用法

策略订阅指定时间框架的K线，处理 JMA 与标准差数值，并根据所设模式自动提交市价单。该实现注重清晰，可作为进一步增强或添加风险管理的基础。

