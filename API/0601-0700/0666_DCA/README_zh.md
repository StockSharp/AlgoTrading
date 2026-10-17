# DCA 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略以固定的报价资产金额定期买入，可以按指定星期几或每隔固定数量的K线执行。到达结束日期后可选择平掉所有仓位。

### 实际调度与数量

默认：`CandleType=5m`、`CapitalToInvest=200` **计价货币单位**、`StartDate=DateTime.MinValue`、`EndDate=DateTime.MaxValue`、`CloseAllOnLastCandle=false`、`UseDayOfWeek=false`、`DayOfWeek=Wednesday`、`EveryXCandles=72`。

只在 `StartDate <= K线日期 < EndDate` 的已完成K线上交易。间隔模式**每72根有效已完成K线**买一次；星期模式在**每周三的首根有效已完成K线**最多买一次。买入量由 `200 / Close` 计算，按 `Security.VolumeStep` **向下取整**并遵守最小/最大量。仅执行市价**买单**，累积多仓；不使用EMA信号，不做空。达到EndDate停止买入；如果 `CloseAllOnLastCandle=true`，在该日或之后首个有效机会卖出**实际剩余仓位**一次。无独立价格止损，滑点可能改变实际成交金额。原说明没有数值默认值，以上是本实现确定的默认配置。



## 参数
- K线类型
- 每次交易投入金额
- 起始日期
- 结束日期
- 在最后一根K线全部平仓
- 使用星期模式
- 星期几
- 每隔X根K线
