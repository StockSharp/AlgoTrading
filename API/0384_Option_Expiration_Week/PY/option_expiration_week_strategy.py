import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math, DayOfWeek, DateTime
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class option_expiration_week_strategy(Strategy):
    """Goes long the specified ETF only during option-expiration week."""

    def __init__(self):
        super().__init__()
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min USD", "Minimum trade value", "Risk")
        self._tf = self.Param("CandleType", tf(1 * 1440)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._latest = {}

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
            raise Exception("ETF not set")
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super().OnReseted()
        self._latest.clear()

    def OnStarted(self, time):
        super().OnStarted(time)
        if self.Security is None:
            raise Exception("ETF cannot be null")
        self.SubscribeCandles(self.candle_type, True, self.Security) \
            .Bind(lambda c, s=self.Security: self._process(c, s)) \
            .Start()

    def _process(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        self._on_daily(candle.OpenTime.Date)

    def _on_daily(self, d):
        in_exp = self._is_exp_week(d)
        port = self.Portfolio.CurrentValue or 0.0
        price = self._latest.get(self.Security, 0)
        tgt = port / price if in_exp and price > 0 else 0
        diff = tgt - self._pos()
        if price <= 0 or abs(diff) * price < self.min_trade_usd:
            return
        side = Sides.Buy if diff > 0 else Sides.Sell
        from StockSharp.BusinessEntities import Order, Security
        self.RegisterOrder(Order(Security=self.Security, Portfolio=self.Portfolio,
                                 Side=side, Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="OpExp"))

    def _pos(self):
        val = self.GetPositionValue(self.Security, self.Portfolio)
        return val or 0.0

    def _is_exp_week(self, d):
        third = DateTime(d.Year, d.Month, 1)
        while third.DayOfWeek != DayOfWeek.Friday:
            third = third.AddDays(1)
        third = third.AddDays(14)
        return d >= third.AddDays(-4) and d <= third

    def CreateClone(self):
        return option_expiration_week_strategy()
