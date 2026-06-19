# GTerminal 策略

## 概述
GTerminal 策略是 MetaTrader 4 专家顾问 `GTerminal_V5a` 的 C# 版本。原始脚本通过在图表上绘制水平线来手动
控制开仓和平仓。本移植版本在 StockSharp 框架中保留了这种基于线条的交互方式，将每条虚拟线暴露为可配置
的参数。当选定周期的收盘价穿越这些虚拟线时，策略会像 MQL4 版本一样开仓、平仓或反向操作。可选的自动
防护参数模拟了原脚本中的 "tpinit" 与 "slinit" 辅助线。

## 策略逻辑
### 价格采样
* 策略使用用户指定周期 (`CandleType`) 的已完成 K 线。
* `StartShift` 决定用于比较的收盘价。值为 `0` 表示使用当前 K 线收盘价，`1` 表示上一根 K 线，以此类推。该
偏移同样作用于比较用的第二根 K 线，因此始终像 MetaTrader 脚本那样比较相邻的两根收盘价。
* `CrossMethod` 与 MQL4 输入保持一致：
  * `0` – 严格穿越：上一根收盘价必须位于触发线的一侧，当前收盘价必须收在另一侧。
  * `1` – 即时触发：只要当前收盘价在触发线另一侧即可。为了避免同一根 K 线多次触发，移植版本仍会检查
上一根收盘价，从而模拟原脚本在触发后删除线条所带来的“只触发一次”效果。

### 入场规则
* **Buy Stop 线** – 当收盘价自下而上突破 `BuyStopLevel` 时买入。如果当前持有空头仓位，订单数量会包含需要
平掉的空头量以及参数 `Volume` 指定的新增多头量。
* **Buy Limit 线** – 当收盘价自上而下跌破 `BuyLimitLevel` 时买入，数量逻辑同上。
* **Sell Stop 线** – 当收盘价自上而下跌破 `SellStopLevel` 时卖出，并一并平掉现有多头仓位。
* **Sell Limit 线** – 当收盘价自下而上突破 `SellLimitLevel` 时卖出建立空头。
* 当 `Volume` 为 `0` 或 `PauseTrading` 为 `true` 时，所有入场信号都会被忽略。

### 出场规则
* **方向性退出** – `LongStopLevel` 与 `LongTakeProfitLevel` 分别在收盘价穿越对应价格时平掉多头；`ShortStopLevel`
与 `ShortTakeProfitLevel` 对空头仓位执行相同逻辑。
* **全局退出** – `AllLongStopLevel` / `AllLongTakeProfitLevel` 无论仓位来源如何都会清空所有多头；`AllShortStopLevel`
/ `AllShortTakeProfitLevel` 针对所有空头执行同样的清仓操作。
* **初始防护** – `UseInitialProtection` 为 `true` 时，在每次新仓成交后立即激活 `InitialLongStopLevel`、`InitialLongTakeProfitLevel`
、`InitialShortStopLevel` 与 `InitialShortTakeProfitLevel`。这些水平相当于原脚本中的 "slinit" / "tpinit" 辅助线，直到
仓位平掉或参数被修改才会失效。
* 每根 K 线只会执行一次退出动作。一旦触发任一退出条件，策略即刻发送平仓单并跳过该根 K 线上剩余的检查，
与 MQL4 脚本在触发线条后停止进一步处理的行为一致。

### 暂停控制
* `PauseTrading` 对应 MetaTrader 中的 "PAUSE" 线。启用时策略不会评估任何入场或出场条件，可在运行期间随时切换。

## 参数
* **Volume** – 新开仓的下单量。在反向建仓时会自动加上需要平掉的反向仓位数量。
* **Cross Method** – 穿越算法选择（`0` 严格、`1` 即时）。
* **Start Shift** – 用于计算穿越的 K 线偏移量。
* **Pause Trading** – 为 `true` 时停止所有交易动作。
* **Use Initial Protection** – 启用后，在每次成交后自动应用初始止损/止盈。
* **Buy Stop Level / Buy Limit Level** – 触发多头建仓的价格。
* **Sell Stop Level / Sell Limit Level** – 触发空头建仓的价格。
* **Long Stop Level / Long Take Profit** – 多头仓位的退出线。
* **Short Stop Level / Short Take Profit** – 空头仓位的退出线。
* **All Long Stop / All Long Take Profit** – 清空所有多头的全局退出线。
* **All Short Stop / All Short Take Profit** – 清空所有空头的全局退出线。
* **Initial Long Stop / Initial Long Take Profit** – 在启用初始防护时应用于新多头的水平。
* **Initial Short Stop / Initial Short Take Profit** – 在启用初始防护时应用于新空头的水平。
* **Candle Type** – 用于比较收盘价的时间周期。

## 实现说明
* 移植版本保留了基于线条的流程，但通过参数而非图表对象来设定价格。用户可在参数面板中实时更新数值，效果
等同于在 MetaTrader 图表上拖动线条。
* 原脚本对 RSI、CCI、Momentum 等指标窗口的联动在本版本中未实现，所有触发均基于收盘价。如需指标驱动行为，可
结合其他 StockSharp 组件自行扩展。
* 策略仅使用市价单（`BuyMarket`, `SellMarket`），与 MQL4 版本通过市价模拟挂单触发的方式一致。
* 本包仅提供 C# 实现，不包含 Python 版本。
