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

from System import DayOfWeek

class btc_dca_ahr999_strategy(Strategy):
    def __init__(self):
        super(btc_dca_ahr999_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))).SetDisplay(
            "CandleType", "Finished daily candle interval", "Parameters")
        self._length = self.Param("Length", 200).SetGreaterThanZero().SetDisplay(
            "Length", "Daily geometric mean window", "Parameters")
        self._usd_invest1 = self.Param("UsdInvest1", 100.0).SetGreaterThanZero().SetDisplay(
            "UsdInvest1", "Budget below AHR999 1.2", "Parameters")
        self._usd_invest2 = self.Param("UsdInvest2", 1000.0).SetGreaterThanZero().SetDisplay(
            "UsdInvest2", "Budget below AHR999 0.45", "Parameters")
        self._start_date = self.Param("StartDate", DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)).SetDisplay(
            "StartDate", "First eligible UTC date", "Parameters")
        self._end_date = self.Param("EndDate", DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc)).SetDisplay(
            "EndDate", "Last eligible UTC date", "Parameters")
        self._reset_state()
        self.OrderRegistering += self._remember_order

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def UsdInvest1(self):
        return self._usd_invest1.Value

    @UsdInvest1.setter
    def UsdInvest1(self, value):
        self._usd_invest1.Value = value

    @property
    def UsdInvest2(self):
        return self._usd_invest2.Value

    @UsdInvest2.setter
    def UsdInvest2(self, value):
        self._usd_invest2.Value = value

    @property
    def StartDate(self):
        return self._start_date.Value

    @StartDate.setter
    def StartDate(self, value):
        self._start_date.Value = value

    @property
    def EndDate(self):
        return self._end_date.Value

    @EndDate.setter
    def EndDate(self, value):
        self._end_date.Value = value

    @property
    def LastAhr999(self):
        return self._last_ahr

    def _reset_state(self):
        self._prices = deque()
        self._last_date = DateTime.MinValue
        self._pending = None
        self._last_ahr = None

    def _remember_order(self, order):
        self._pending = order

    def OnReseted(self):
        super(btc_dca_ahr999_strategy, self).OnReseted()
        self._reset_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        if self.CandleType != DataType.TimeFrame(TimeSpan.FromDays(1)) or self.StartDate.Kind != DateTimeKind.Utc or self.EndDate.Kind != DateTimeKind.Utc or self.StartDate > self.EndDate:
            raise ValueError("Daily candles and an ordered UTC date range are required.")
        super(btc_dca_ahr999_strategy, self).OnStarted2(time)
        candles = self.SubscribeCandles(self.CandleType)
        candles.Bind(self._process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    def _process(self, candle):
        if candle.State != CandleStates.Finished or candle.ClosePrice <= 0 or candle.OpenTime.Date <= self._last_date:
            return
        date = candle.OpenTime.Date
        self._last_date = date
        close = float(candle.ClosePrice)
        self._prices.append(close)
        while len(self._prices) > self.Length:
            self._prices.popleft()
        if len(self._prices) < self.Length:
            return
        age = (date - DateTime(2009, 1, 3, 0, 0, 0, DateTimeKind.Utc)).TotalDays
        if age <= 0:
            return
        geometric = math.exp(sum(math.log(p) for p in self._prices) / self.Length)
        fitted = 10.0 ** (5.84 * math.log10(age) - 17.01)
        self._last_ahr = close / geometric * close / fitted
        if not self.IsFormedAndOnlineAndAllowTrading() or date < self.StartDate.Date or date > self.EndDate.Date or date.DayOfWeek != DayOfWeek.Monday:
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return
        budget = float(self.UsdInvest2) if self._last_ahr < 0.45 else float(self.UsdInvest1) if self._last_ahr < 1.2 else 0.0
        if budget == 0:
            return
        step = float(self.Security.VolumeStep or 1)
        quantity = math.floor(budget / close / step) * step
        if quantity < float(self.Security.MinVolume or step):
            return
        if self.Security.MaxVolume is not None and quantity > float(self.Security.MaxVolume):
            raise ValueError("The investment exceeds the instrument's maximum order volume.")
        self.BuyMarket(Decimal(quantity))

    def CreateClone(self):
        return btc_dca_ahr999_strategy()
