import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, DayOfWeek, DateTime
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class payday_anomaly_strategy(Strategy):
    """Holds market ETF during days around typical payday window."""

    def __init__(self):
        super(payday_anomaly_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._last = None
        self._entered_month_key = 0
        self._exited_month_key = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(payday_anomaly_strategy, self).OnReseted()
        self._last = None
        self._entered_month_key = 0
        self._exited_month_key = 0

    def OnStarted(self, time):
        super(payday_anomaly_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        d = candle.OpenTime.Date
        if d == self._last:
            return
        self._last = d

        month_key = d.Year * 100 + d.Month
        td_end = self._trading_days_left(d)
        td_start = self._trading_day_number(d)
        in_window = td_end <= 2 or td_start <= 3

        if in_window and self.Position == 0 and self._entered_month_key != month_key:
            self.BuyMarket()
            self._entered_month_key = month_key
            self._exited_month_key = 0
        elif not in_window and self.Position > 0 and self._entered_month_key == month_key and self._exited_month_key != month_key:
            self.SellMarket()
            self._exited_month_key = month_key

    def _trading_days_left(self, d):
        cnt = 0
        cur = d
        while cur.Month == d.Month:
            if cur.DayOfWeek != DayOfWeek.Saturday and cur.DayOfWeek != DayOfWeek.Sunday:
                cnt += 1
            cur = cur.AddDays(1)
        return cnt - 1

    def _trading_day_number(self, d):
        num = 0
        cur = DateTime(d.Year, d.Month, 1)
        while cur <= d:
            if cur.DayOfWeek != DayOfWeek.Saturday and cur.DayOfWeek != DayOfWeek.Sunday:
                num += 1
            cur = cur.AddDays(1)
        return num

    def CreateClone(self):
        return payday_anomaly_strategy()
