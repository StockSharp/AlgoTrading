import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import ArgumentException, DateTime, DateTimeKind, Decimal, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage, SuperTrend
from StockSharp.Algo.Strategies import Strategy


class pivot_point_super_trend_trend_filter_strategy(Strategy):
    def __init__(self):
        super(pivot_point_super_trend_trend_filter_strategy, self).__init__()
        self._pivot_period = self.Param("PivotPeriod", 2).SetGreaterThanZero() \
            .SetDisplay("Pivot Period", "Finished candles on each side of a confirmed pivot", "Indicators")
        self._factor = self.Param("Factor", Decimal.Parse("3")).SetGreaterThanZero() \
            .SetDisplay("Factor", "ATR width of the pivot bands", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 10).SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Wilder ATR period of the pivot bands", "Indicators")
        self._trend_atr_period = self.Param("TrendAtrPeriod", 10).SetGreaterThanZero() \
            .SetDisplay("Trend ATR Period", "Wilder ATR period of the price SuperTrend", "Indicators")
        self._trend_multiplier = self.Param("TrendMultiplier", Decimal.Parse("3")).SetGreaterThanZero() \
            .SetDisplay("Trend Multiplier", "ATR width of the price SuperTrend", "Indicators")
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero() \
            .SetDisplay("MA Period", "Close-price SMA period for trend-filter entries", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of finished candles", "General")
        self._start_date = self.Param("StartDate", DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)) \
            .SetDisplay("Start Date", "Inclusive UTC candle-open time for pivot entries", "General")
        self._end_date = self.Param("EndDate", DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)) \
            .SetDisplay("End Date", "Inclusive UTC candle-open time for pivot entries", "General")
        self._reset_signals()

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def _reset_signals(self):
        self._pivots = []
        self._center = None
        self._support = None
        self._resistance = None
        self._previous_close = None
        self._pivot_up = None
        self._filter_up = None

    def OnReseted(self):
        super(pivot_point_super_trend_trend_filter_strategy, self).OnReseted()
        self._reset_signals()

    def OnStarted2(self, time):
        super(pivot_point_super_trend_trend_filter_strategy, self).OnStarted2(time)
        start = self._start_date.Value
        end = self._end_date.Value
        if start.Kind != DateTimeKind.Utc or end.Kind != DateTimeKind.Utc or start > end:
            raise ArgumentException("The pivot entry window requires ordered UTC dates.")
        self._reset_signals()
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        trend = SuperTrend()
        trend.Length = self._trend_atr_period.Value
        trend.Multiplier = self._trend_multiplier.Value
        average = SimpleMovingAverage()
        average.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, trend, average, self.OnProcess, True).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, trend)
            self.DrawIndicator(area, average)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_value, trend_value, average_value):
        if candle.State != CandleStates.Finished:
            return
        previous_close = candle.ClosePrice if self._previous_close is None else self._previous_close
        self._previous_close = candle.ClosePrice
        self._pivots.append((candle.HighPrice, candle.LowPrice))
        period = self._pivot_period.Value
        if len(self._pivots) > 2 * period + 1:
            self._pivots.pop(0)
        if len(self._pivots) == 2 * period + 1:
            candidate = self._pivots[period]
            neighbors = self._pivots[:period] + self._pivots[period + 1:]
            is_high = all(candidate[0] > bar[0] for bar in neighbors)
            is_low = all(candidate[1] < bar[1] for bar in neighbors)
            if is_high or is_low:
                pivot = candidate[0] if is_high else candidate[1]
                self._center = pivot if self._center is None else (Decimal(2) * self._center + pivot) / Decimal(3)
        previous_pivot_up = self._pivot_up
        if self._center is not None and atr_value.IsFormed and not atr_value.IsEmpty:
            width = self._factor.Value * atr_value.GetValue[Decimal](None)
            old_support = self._support
            old_resistance = self._resistance
            basic_support = self._center - width
            basic_resistance = self._center + width
            self._support = max(basic_support, old_support) if old_support is not None and previous_close > old_support else basic_support
            self._resistance = min(basic_resistance, old_resistance) if old_resistance is not None and previous_close < old_resistance else basic_resistance
            if old_resistance is not None and candle.ClosePrice > old_resistance:
                self._pivot_up = True
            elif old_support is not None and candle.ClosePrice < old_support:
                self._pivot_up = False
            elif self._pivot_up is None:
                self._pivot_up = True
        previous_filter_up = self._filter_up
        if trend_value.IsFormed and not trend_value.IsEmpty:
            self._filter_up = bool(trend_value.IsUpTrend)
        if self._pivot_up is None or self._filter_up is None or not average_value.IsFormed \
                or average_value.IsEmpty or not self.IsFormedAndOnlineAndAllowTrading():
            return
        pivot_buy = previous_pivot_up is False and self._pivot_up
        pivot_sell = previous_pivot_up is True and not self._pivot_up
        filter_buy = previous_filter_up is False and self._filter_up
        filter_sell = previous_filter_up is True and not self._filter_up
        within_dates = self._start_date.Value <= candle.OpenTime <= self._end_date.Value
        up_signal = pivot_buy or filter_buy
        down_signal = pivot_sell or filter_sell
        average_price = average_value.GetValue[Decimal](None)
        enter_long = not down_signal and (filter_buy and candle.ClosePrice > average_price or pivot_buy and within_dates)
        enter_short = not up_signal and (filter_sell and candle.ClosePrice < average_price or pivot_sell and within_dates)
        if self.Position > 0 and down_signal:
            self.SellMarket(self.Position + (self.Volume if enter_short else Decimal.Zero))
        elif self.Position < 0 and up_signal:
            self.BuyMarket(-self.Position + (self.Volume if enter_long else Decimal.Zero))
        elif self.Position == 0 and enter_long:
            self.BuyMarket(self.Volume)
        elif self.Position == 0 and enter_short:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return pivot_point_super_trend_trend_filter_strategy()
