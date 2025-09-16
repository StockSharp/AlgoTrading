# GG-RSI-CCI 策略

该策略使用 StockSharp 高级 API 复现 **GG-RSI-CCI** 指标策略。
它结合 RSI 与 CCI 指标，并分别使用快慢均线进行平滑。当两个指标方向一致时开仓。

## 逻辑

1. **指标**
   - 计算相同周期的 RSI 和 CCI。
   - 每个指标再分别用快慢移动平均线进行平滑处理。
2. **信号**
   - 当 RSI 快线高于慢线且 CCI 快线高于慢线时买入。
   - 当 RSI 快线低于慢线且 CCI 快线低于慢线时卖出。
   - 若模式设为 `Flat`，出现中性状态时立即平仓。
3. **风险管理**
   - 启动时调用 `StartProtection`。止损和止盈可通过平台的风险管理器设置。

## 参数

| 名称            | 说明                         |
|-----------------|------------------------------|
| `CandleType`    | 计算所用的时间框架。          |
| `Length`        | RSI 与 CCI 的周期。           |
| `FastPeriod`    | 快速平滑周期。                |
| `SlowPeriod`    | 慢速平滑周期。                |
| `Volume`        | 下单数量。                    |
| `AllowBuyOpen`  | 允许开多。                    |
| `AllowSellOpen` | 允许开空。                    |
| `AllowBuyClose` | 允许平空。                    |
| `AllowSellClose`| 允许平多。                    |
| `Mode`          | `Trend` 仅在反向信号时平仓，`Flat` 在中性信号也平仓。 |

## 说明

策略仅处理已完成的 K 线，并使用 `BuyMarket`/`SellMarket` 进行交易。
指标准确值保存在内部变量中，不直接访问指示器缓冲区。
