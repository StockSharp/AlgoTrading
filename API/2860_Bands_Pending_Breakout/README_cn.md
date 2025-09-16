# Bands Pending Breakout 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 的 “Bands 2” 专家系统迁移到 StockSharp 高级 API。它只在蜡烛收盘后运行，确认当前时间处于设定的交易时段且收盘价落在布林带上下轨之间时，会在通道两侧各挂三张对称的买入 Stop 和卖出 Stop 网格单。每张挂单携带独立的止损与止盈距离，一旦任意挂单成交，其余挂单即刻撤单。

策略针对布林带区间的突破行情。止损可以选择参照对侧布林带或中心移动平均线。另有拖尾模块，当浮盈达到设定阈值后自动上移/下移保护止损。

## 策略细节

- **行情源**：支持任何通过 StockSharp 提供的品种与蜡烛类型。
- **交易时段**：`HourStart`/`HourEnd` 限制只在指定时段内重新部署挂单，每根收盘蜡烛都会刷新一次。
- **入场逻辑**：
  - 仅处理状态为 Finished 的蜡烛，要求收盘价严格位于（可选前移的）布林带上下轨之间。
  - 撤销上一根蜡烛遗留的挂单，并在上轨之上、下轨之下分别布置三层 buy stop / sell stop 网格。
  - 层与层之间的间距由 `StepPips` （换算为最小价格步长）决定。
- **止损模式**：
  - *BollingerBands*：止损放在对侧布林带并加上同样的步进距离。
  - *MovingAverage*：止损放在移动平均线附近，使用配置的周期、价格类型和 `MaShift` 前移量。
  - *None*：不设置初始止损，后续由拖尾止损决定。
- **止盈逻辑**：
  - 第一层买卖单都使用 `FirstTakeProfitPips`。
  - 第二、第三层买单分别使用 `Second`、`Third` 止盈；卖单保持与原始 MQL 脚本一致，仍采用第一层止盈距离。
- **订单管理**：
  - 任何挂单成交后，立即撤销其他未成交挂单，并为当前仓位发送保护性 stop/limit 指令。
  - 拖尾模块在价格向有利方向推进 `TrailingStopPips + TrailingStepPips` 之后移动止损。
  - 仓位平仓时自动撤销尚存的保护指令。
- **价格归一化**：所有价格都会对齐到最小跳动点，点值转换逻辑与原 EA 在 3/5 位报价上的处理保持一致。

## 参数说明

| 参数 | 说明 |
|------|------|
| `OrderVolume` | 每张挂单的下单量，六张挂单使用相同的数量。 |
| `CandleType` | 计算指标所使用的蜡烛/数据类型。 |
| `HourStart`, `HourEnd` | 允许下单的小时区间（0–24）。`HourEnd` 必须大于 `HourStart`。 |
| `StopLossMode` | 初始止损的定位方式（布林带对侧、移动平均或不设置）。 |
| `FirstTakeProfitPips`, `SecondTakeProfitPips`, `ThirdTakeProfitPips` | 三层网格的止盈距离（以点计），会自动换算成价格。 |
| `TrailingStopPips`, `TrailingStepPips` | 拖尾止损的距离与触发额外步长，为 0 时关闭拖尾。 |
| `StepPips` | 相邻挂单之间的点距。 |
| `MaPeriod`, `MaShift`, `MaMethod`, `MaPriceType` | 移动平均线参数（周期、前移、算法、价格类型），`MaShift` 模拟原 EA 的向前平移。 |
| `BandsPeriod`, `BandsShift`, `BandsDeviation`, `BandsPriceType` | 布林带参数（周期、前移、标准差倍数、价格类型）。 |

## 执行流程

1. 订阅所选时间框架的蜡烛并等待收盘。
2. 在交易时段内对每根收盘蜡烛计算布林带与移动平均（考虑配置的前移）。
3. 若收盘价位于通道内，则按照设置的步距布置买/卖 stop 网格，并附带对应的止损与止盈。
4. 挂单成交后撤销其他挂单，提交保护性止损/止盈，并根据拖尾参数动态调整。
5. 仓位平仓后撤销保护指令，等待下一次突破机会。
