import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

import math
from collections import deque
from System import TimeSpan, DateTime, DateTimeKind, Decimal, Nullable
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates, Unit, UnitTypes
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class btc_outperform_strategy(Strategy):
    def __init__(self):
        super(btc_outperform_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))).SetDisplay(
            "CandleType", "Finished source candle interval", "Parameters")
        self._weekly_days = self.Param("WeeklyDays", 7).SetGreaterThanZero().SetDisplay(
            "WeeklyDays", "UTC Monday-anchored weekly period in days", "Parameters")
        self._quarterly_days = self.Param("QuarterlyDays", 90).SetGreaterThanZero().SetDisplay(
            "QuarterlyDays", "UTC epoch-anchored long period in days", "Parameters")
        self._reset_state()
        self.OrderRegistering += self._remember_order

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def WeeklyDays(self):
        return self._weekly_days.Value

    @WeeklyDays.setter
    def WeeklyDays(self, value):
        self._weekly_days.Value = value

    @property
    def QuarterlyDays(self):
        return self._quarterly_days.Value

    @QuarterlyDays.setter
    def QuarterlyDays(self, value):
        self._quarterly_days.Value = value

    def _reset_state(self):
        self._week = DateTime.MinValue
        self._quarter = DateTime.MinValue
        self._last_time = DateTime.MinValue
        self._week_count = self._quarter_count = 0
        self._weekly_close = self._quarterly_close = None
        self._pending = None

    def _remember_order(self, order):
        self._pending = order

    def OnReseted(self):
        super(btc_outperform_strategy, self).OnReseted()
        self._reset_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        if self.CandleType != DataType.TimeFrame(TimeSpan.FromDays(1)) or self.WeeklyDays >= self.QuarterlyDays:
            raise ValueError("Daily source candles and a weekly period shorter than the quarterly period are required.")
        super(btc_outperform_strategy, self).OnStarted2(time)
        candles = self.SubscribeCandles(self.CandleType)
        candles.Bind(self._process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    @staticmethod
    def _bucket(date, epoch, days):
        return epoch.AddDays(math.floor((date - epoch).TotalDays / days) * days)

    def _process(self, candle):
        if candle.State != CandleStates.Finished or candle.OpenTime <= self._last_time:
            return
        date = candle.OpenTime.Date
        contiguous = self._last_time == DateTime.MinValue or date == self._last_time.AddDays(1)
        week = self._bucket(date, DateTime(1970, 1, 5, 0, 0, 0, DateTimeKind.Utc), self.WeeklyDays)
        quarter = self._bucket(date, DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), self.QuarterlyDays)
        self._week_count = (1 if date == week else 0) if week != self._week else (self._week_count + 1 if contiguous and self._week_count > 0 else 0)
        self._quarter_count = (1 if date == quarter else 0) if quarter != self._quarter else (self._quarter_count + 1 if contiguous and self._quarter_count > 0 else 0)
        self._week, self._quarter, self._last_time = week, quarter, date
        if self._week_count == self.WeeklyDays and date.AddDays(1) == week.AddDays(self.WeeklyDays):
            self._weekly_close = candle.ClosePrice
        if self._quarter_count == self.QuarterlyDays and date.AddDays(1) == quarter.AddDays(self.QuarterlyDays):
            self._quarterly_close = candle.ClosePrice
        if self._weekly_close is None or self._quarterly_close is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if self._weekly_close > self._quarterly_close and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self._weekly_close < self._quarterly_close and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return btc_outperform_strategy()
