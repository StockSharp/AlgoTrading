import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math, DayOfWeek, DateTime, TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class payday_anomaly_strategy(Strategy):
    """Holds market ETF during days around typical payday window."""

    def __init__(self):
        super().__init__()
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min Trade USD", "Minimum trade value in USD", "General")
        self._tf = self.Param("CandleType", tf(1 * 1440)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._latest = {}
        self._last = None

    @property
    def min_trade_usd(self):
        return self._min_usd.Value

    @min_trade_usd.setter
    def min_trade_usd(self, value):
        self._min_usd.Value = value

    @property
    def candle_type(self):
        return self._tf.Value

    @candle_type.setter
    def candle_type(self, value):
        self._tf.Value = value

    def GetWorkingSecurities(self):
        if self.Security is None:
            raise Exception("Security not set")
        yield self.Security, self.candle_type

    def OnReseted(self):
        super().OnReseted()
        self._latest.clear()
        self._last = None

    def OnStarted(self, time):
        if self.Security is None:
            raise Exception("Security not set")
        super().OnStarted(time)
        self.SubscribeCandles(self.candle_type, True, self.Security).Bind(lambda c, s=self.Security: self._process(c, s)).Start()

    def _process(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        self._on_daily(candle.OpenTime.Date)

    def _on_daily(self, d):
        if self._last == d:
            return
        self._last = d
        td_end = self._trading_days_left(d)
        td_start = self._trading_day_number(d)
        in_window = td_end <= 2 or td_start <= 3
        port = self.Portfolio.CurrentValue or 0.0
        price = self._latest.get(self.Security, 0)
        tgt = port / price if in_window and price > 0 else 0
        diff = tgt - self._pos()
        if price <= 0 or abs(diff) * price < self.min_trade_usd:
            return
        side = Sides.Buy if diff > 0 else Sides.Sell
        from StockSharp.BusinessEntities import Order, Security
        self.RegisterOrder(Order(Security=self.Security, Portfolio=self.Portfolio, Side=side,
                                 Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="Payday"))

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

    def _pos(self):
        val = self.GetPositionValue(self.Security, self.Portfolio)
        return val or 0.0

    def CreateClone(self):
        return payday_anomaly_strategy()
