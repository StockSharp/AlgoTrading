import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, DayOfWeek
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from datatype_extensions import *


class bitcoin_intraday_seasonality_strategy(Strategy):
    """
    Strategy that goes long on Bitcoin during predefined strong intraday hours.
    Only trades on the first Monday of the month during specified UTC hours.
    """

    def __init__(self):
        super(bitcoin_intraday_seasonality_strategy, self).__init__()

        self._hours_long = self.Param("HoursLong", [0, 1, 2, 3]) \
            .SetDisplay("Long Hours", "UTC hours when the strategy stays long", "General")

        self._min_trade_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum order value in USD", "Trading")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._latest_price = 0.0

    @property
    def HoursLong(self):
        return self._hours_long.Value

    @HoursLong.setter
    def HoursLong(self, value):
        self._hours_long.Value = value

    @property
    def MinTradeUsd(self):
        return self._min_trade_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_trade_usd.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(bitcoin_intraday_seasonality_strategy, self).OnReseted()
        self._latest_price = 0.0

    def OnStarted(self, time):
        super(bitcoin_intraday_seasonality_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_price = float(candle.ClosePrice)

        hour = candle.OpenTime.Hour
        is_first_monday = candle.OpenTime.DayOfWeek == DayOfWeek.Monday and candle.OpenTime.Day <= 7
        in_season = is_first_monday and hour in self.HoursLong

        price = self._latest_price
        if price <= 0:
            return

        portfolio_value = 0.0
        if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
            portfolio_value = float(self.Portfolio.CurrentValue)

        if portfolio_value <= 0:
            portfolio_value = 100000.0

        tgt = portfolio_value / price if in_season else 0.0
        diff = tgt - float(self.Position)

        if abs(diff) * price < self.MinTradeUsd:
            return

        if diff > 0:
            self.BuyMarket(abs(diff))
        elif diff < 0:
            self.SellMarket(abs(diff))

    def CreateClone(self):
        return bitcoin_intraday_seasonality_strategy()
