import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class monthly_breakout_strategy(Strategy):
    def __init__(self):
        super(monthly_breakout_strategy, self).__init__()
        self._entry_option = self.Param("EntryOption", 2) \
            .SetDisplay("Entry Option", "0=LongAtHigh,1=ShortAtHigh,2=LongAtLow,3=ShortAtLow", "General")
        self._holding_period = self.Param("HoldingPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Holding Period", "Bars to hold position", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Working candle timeframe", "General")
        self._monthly_high = 0.0
        self._monthly_low = 0.0
        self._prev_monthly_high = 0.0
        self._prev_monthly_low = 0.0
        self._prev_close = 0.0
        self._current_week = 0
        self._bar_index = 0
        self._entry_bar = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(monthly_breakout_strategy, self).OnReseted()
        self._monthly_high = 0.0
        self._monthly_low = 0.0
        self._prev_monthly_high = 0.0
        self._prev_monthly_low = 0.0
        self._prev_close = 0.0
        self._current_week = 0
        self._bar_index = 0
        self._entry_bar = -1

    def OnStarted(self, time):
        super(monthly_breakout_strategy, self).OnStarted(time)
        self._monthly_high = 0.0
        self._monthly_low = 0.0
        self._prev_monthly_high = 0.0
        self._prev_monthly_low = 0.0
        self._prev_close = 0.0
        self._current_week = 0
        self._bar_index = 0
        self._entry_bar = -1
        dummy1 = ExponentialMovingAverage()
        dummy1.Length = 10
        dummy2 = ExponentialMovingAverage()
        dummy2.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(dummy1, dummy2, self.OnProcess).Start()

    def _get_week(self, dt):
        day_of_year = dt.DayOfYear
        return (day_of_year - 1) // 7

    def OnProcess(self, candle, d1, d2):
        if candle.State != CandleStates.Finished:
            return
        self._bar_index += 1
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        week = self._get_week(candle.OpenTime)
        if week != self._current_week:
            if self._current_week != 0:
                self._prev_monthly_high = self._monthly_high
                self._prev_monthly_low = self._monthly_low
            self._monthly_high = high
            self._monthly_low = low
            self._current_week = week
        else:
            if high > self._monthly_high:
                self._monthly_high = high
            if low < self._monthly_low:
                self._monthly_low = low
        if self._prev_monthly_high <= 0.0 or self._prev_monthly_low <= 0.0:
            self._prev_close = close
            return
        cross_above_high = self._prev_close <= self._prev_monthly_high and close > self._prev_monthly_high
        cross_below_high = self._prev_close >= self._prev_monthly_high and close < self._prev_monthly_high
        cross_above_low = self._prev_close <= self._prev_monthly_low and close > self._prev_monthly_low
        cross_below_low = self._prev_close >= self._prev_monthly_low and close < self._prev_monthly_low
        hp = self._holding_period.Value
        if self.Position != 0 and self._entry_bar >= 0 and self._bar_index >= self._entry_bar + hp:
            if self.Position > 0:
                self.SellMarket()
            else:
                self.BuyMarket()
            self._entry_bar = -1
        opt = self._entry_option.Value
        if opt == 0 and cross_above_high and self.Position <= 0:
            self.BuyMarket()
            self._entry_bar = self._bar_index
        elif opt == 1 and cross_below_high and self.Position >= 0:
            self.SellMarket()
            self._entry_bar = self._bar_index
        elif opt == 2 and cross_above_low and self.Position <= 0:
            self.BuyMarket()
            self._entry_bar = self._bar_index
        elif opt == 3 and cross_below_low and self.Position >= 0:
            self.SellMarket()
            self._entry_bar = self._bar_index
        self._prev_close = close

    def CreateClone(self):
        return monthly_breakout_strategy()
