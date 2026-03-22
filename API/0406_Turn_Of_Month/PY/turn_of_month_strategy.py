import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, DayOfWeek, DateTime, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class turn_of_month_strategy(Strategy):
    """Turn-of-the-month effect strategy for index ETFs."""

    def __init__(self):
        super(turn_of_month_strategy, self).__init__()

        self._prior = self.Param("DaysPrior", 2) \
            .SetDisplay("Days Prior", "Trading days before month end", "Parameters")
        self._after = self.Param("DaysAfter", 4) \
            .SetDisplay("Days After", "Trading days into new month", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(turn_of_month_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(turn_of_month_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        d = candle.OpenTime.Date
        td_left = self._trading_days_left(d)
        td_num = self._trading_day_number(d)
        days_prior = int(self._prior.Value)
        days_after = int(self._after.Value)
        cooldown = int(self._cooldown_bars.Value)

        in_window = td_left <= days_prior or td_num <= days_after

        if in_window and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif not in_window and self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def _trading_days_left(self, d):
        cnt = 0
        cur = d
        while cur.Month == d.Month:
            if cur.DayOfWeek != DayOfWeek.Saturday and cur.DayOfWeek != DayOfWeek.Sunday:
                cnt += 1
            cur = cur.AddDays(1)
        return cnt - 1

    def _trading_day_number(self, d):
        n = 0
        cur = DateTime(d.Year, d.Month, 1)
        while cur <= d:
            if cur.DayOfWeek != DayOfWeek.Saturday and cur.DayOfWeek != DayOfWeek.Sunday:
                n += 1
            cur = cur.AddDays(1)
        return n

    def CreateClone(self):
        return turn_of_month_strategy()
