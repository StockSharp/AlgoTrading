import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, DayOfWeek, DateTime
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class option_expiration_week_strategy(Strategy):
    """Goes long the specified ETF only during option-expiration week."""

    def __init__(self):
        super(option_expiration_week_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._entered_month_key = 0
        self._exited_month_key = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(option_expiration_week_strategy, self).OnReseted()
        self._entered_month_key = 0
        self._exited_month_key = 0

    def OnStarted(self, time):
        super(option_expiration_week_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        d = candle.OpenTime.Date
        month_key = d.Year * 100 + d.Month
        in_exp = self._is_exp_week(d)

        if in_exp and self.Position == 0 and self._entered_month_key != month_key:
            self.BuyMarket()
            self._entered_month_key = month_key
            self._exited_month_key = 0
        elif not in_exp and self.Position > 0 and self._entered_month_key == month_key and self._exited_month_key != month_key:
            self.SellMarket()
            self._exited_month_key = month_key

    def _is_exp_week(self, d):
        third = DateTime(d.Year, d.Month, 1)
        while third.DayOfWeek != DayOfWeek.Friday:
            third = third.AddDays(1)
        third = third.AddDays(14)
        return d >= third.AddDays(-4) and d <= third

    def CreateClone(self):
        return option_expiration_week_strategy()
