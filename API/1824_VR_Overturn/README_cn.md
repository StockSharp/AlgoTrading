# VR Overturn 策略

**VR Overturn** 实现了简单的马丁格尔与反马丁格尔逻辑。
策略始终只持有一个仓位，仓位平仓后会立即根据上一笔交易
的结果开立新的仓位。

## 策略逻辑

1. 按照 `StartSide` 方向以 `StartVolume` 手数开仓。
2. 以点数偏移设置止损与止盈。
3. 仓位平仓后：
   - 计算上一笔交易的盈亏。
   - **Martingale** 模式：
     - 盈利 → 手数重置为 `StartVolume`，方向保持不变。
     - 亏损 → 手数乘以 `Multiplier`，方向反转。
   - **AntiMartingale** 模式：
     - 盈利 → 手数乘以 `Multiplier`，方向保持不变。
     - 亏损 → 手数重置为 `StartVolume`，方向反转。
4. 使用计算出的方向和手数开立下一笔仓位。

该过程在策略运行期间持续重复。

## 参数

| 名称 | 描述 |
|------|------|
| `Mode` | 交易模式：`Martingale` 或 `AntiMartingale`。 |
| `StartSide` | 首笔交易方向（`Buy` 或 `Sell`）。 |
| `TakeProfit` | 距离入场价的止盈点数。 |
| `StopLoss` | 距离入场价的止损点数。 |
| `StartVolume` | 首笔交易使用的初始手数。 |
| `Multiplier` | 盈利或亏损后应用的手数倍数。 |

## 备注

- 保护性订单以止损和限价订单形式发送。
- 任意时刻只有一个仓位。
- 策略不使用任何市场指标。
