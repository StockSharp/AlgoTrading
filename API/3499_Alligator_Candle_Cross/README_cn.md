# Alligator Candle Cross 策略

该策略将 MetaTrader 专家顾问 **alligator candle cross up/down** 移植到 StockSharp 高阶 API。策略基于 Bill Williams 的 Alligator 指标，使用对称均价 `(High + Low) / 2` 计算的平滑移动平均线，并在收盘蜡烛的实体穿越 Alligator “嘴部” 时触发信号。通过参数可以选择仅做多、仅做空或双向交易，风险控制依赖固定的止损与止盈点数。

## 交易逻辑

### 指标计算
- 使用经典周期 13/8/5 的平滑移动平均线生成 Alligator 的 **Jaw**、**Teeth**、**Lips** 三条线。
- 默认对三条线应用 8/5/3 根 K 线的正向偏移，使得比较的是位于指标前方的蜡烛。
- 所有计算均采用蜡烛中值 `(High + Low) / 2`，与 MetaTrader 版本保持一致。

### 多头条件（“candle cross up”）
1. 上一根完成的蜡烛收盘价位于三条 Alligator 线中最靠下的值之下或相等（考虑偏移）。
2. 当前蜡烛实体开盘价不高于三条线的最高值，且收盘价高于同一数值，说明实体向上穿过 Alligator。
3. 当前没有持仓，并且允许交易。
4. 满足条件时以设定的交易量市价买入。

### 空头条件（“candle cross down”）
1. 上一根蜡烛的收盘价位于三条 Alligator 线中最高值之上或相等。
2. 当前蜡烛实体开盘价不低于三条线的最低值，收盘价低于该值，确认向下穿越。
3. 当前没有持仓并允许交易。
4. 满足条件时以设定的交易量市价卖出。

### 仓位管理
- 开仓后将止损和止盈从点数换算为绝对价格，使用标的资产的最小价位步长。
- 多头仓位在触发止损、触发止盈，或蜡烛收盘价跌破偏移后的 Teeth 与 Lips 的最小值时平仓。
- 空头仓位在触发止损、触发止盈，或蜡烛收盘价升破偏移后的 Teeth 与 Lips 的最大值时平仓。
- 启动时调用 **StartProtection**，用于防止异常成交造成的风险。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| ---- | ---- | ------ | ---- |
| `OrderVolume` | `decimal` | `0.1` | 交易手数或合约数量。 |
| `StopLossPips` | `int` | `50` | 距离进场价的止损点数，0 表示禁用。 |
| `TakeProfitPips` | `int` | `50` | 距离进场价的止盈点数，0 表示禁用。 |
| `JawPeriod` | `int` | `13` | Alligator 下颚（蓝线）的平滑均线周期。 |
| `JawShift` | `int` | `8` | 下颚线向前偏移的根数。 |
| `TeethPeriod` | `int` | `8` | Alligator 牙齿（红线）的平滑均线周期。 |
| `TeethShift` | `int` | `5` | 牙齿线的前移根数。 |
| `LipsPeriod` | `int` | `5` | Alligator 嘴唇（绿线）的平滑均线周期。 |
| `LipsShift` | `int` | `3` | 嘴唇线的前移根数。 |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | 用于计算的蜡烛时间框架。 |
| `EntryMode` | `AlligatorCrossMode` | `Both` | 选择仅做多、仅做空或双向交易。 |

## 使用说明
- 适用于 StockSharp 支持的所有品种；请根据原始 MetaTrader 模板设置相同的 `CandleType`。
- 点值根据品种的报价精度自动推断：对于 3 或 5 位小数的货币对，一个点等于十个最小报价步长。
- 策略只处理已完成的蜡烛，不依赖逐笔数据，便于与 MetaTrader 回测结果对齐。
