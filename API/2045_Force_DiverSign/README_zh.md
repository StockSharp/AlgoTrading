# Force DiverSign 策略

Force DiverSign 策略基于两个使用不同平滑周期的 Force Index 指标之间的背离信号进行交易。
该策略寻找由三根K线组成的反转形态，并结合快慢 Force 值的相反摆动。
出现看涨背离时买入，出现看跌背离时卖出。

## 参数
- `Period1` – 快速 Force Index 的周期。
- `Period2` – 慢速 Force Index 的周期。
- `MaType1` – 用于平滑快速 Force Index 的均线类型。
- `MaType2` – 用于平滑慢速 Force Index 的均线类型。
- `CandleType` – 计算所用的K线时间框架。

## 交易逻辑
1. 计算原始 Force Index：成交量乘以收盘价变化。
2. 使用两条均线对原始值进行平滑，得到快慢两个 Force 序列。
3. 保存最近五个 Force 值和最近四根K线。
4. **买入** 条件：
   - 前三根K线形成下–上–下的形态；
   - 两个 Force 序列形成局部底并随后上升；
   - 第一次和第三次K线之间快慢 Force 方向相反。
5. **卖出** 条件：
   - 前三根K线形成上–下–上的形态；
   - 两个 Force 序列形成局部顶并随后下降；
   - 第一次和第三次K线之间快慢 Force 方向相反。

每个信号都会反转持仓：买入平掉空头，卖出平掉多头。
