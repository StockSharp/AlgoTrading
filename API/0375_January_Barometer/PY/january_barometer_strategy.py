import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order
from datatype_extensions import *


class january_barometer_strategy(Strategy):
    """January barometer strategy rotating between equity and cash ETFs."""

    def __init__(self):
        super(january_barometer_strategy, self).__init__()

        self._equity = self.Param[Security]("EquityETF", None) \
            .SetDisplay("Equity ETF", "Risk asset", "General")

        self._cash = self.Param[Security]("CashETF", None) \
            .SetDisplay("Cash ETF", "Safe asset", "General")

        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min trade USD", "Minimum order value", "Risk")

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._latest_prices = {}
        self._jan_open_price = 0.0

    # region properties
    @property
    def EquityETF(self):
        return self._equity.Value

    @EquityETF.setter
    def EquityETF(self, value):
        self._equity.Value = value

    @property
    def CashETF(self):
        return self._cash.Value

    @CashETF.setter
    def CashETF(self, value):
        self._cash.Value = value

    @property
    def MinTradeUsd(self):
        return self._min_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_usd.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value
    # endregion

    def GetWorkingSecurities(self):
        if self.EquityETF is None or self.CashETF is None:
            raise Exception("Both equity and cash ETFs must be set.")
        return [(self.EquityETF, self.CandleType)]

    def OnReseted(self):
        super(january_barometer_strategy, self).OnReseted()
        self._latest_prices.clear()
        self._jan_open_price = 0.0

    def OnStarted(self, time):
        if self.EquityETF is None or self.CashETF is None:
            raise Exception("Both equity and cash ETFs must be set.")

        super(january_barometer_strategy, self).OnStarted(time)

        self.SubscribeCandles(self.CandleType, True, self.EquityETF) \
            .Bind(lambda candle: self.ProcessCandle(candle, self.EquityETF)) \
            .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice
        self.OnDaily(candle)

    def OnDaily(self, candle):
        d = candle.OpenTime.Date
        if d.Month == 1 and d.Day == 1:
            self._jan_open_price = candle.OpenPrice

        if d.Month == 1 and (d.Day == 31 or candle.CloseTime.Date.Month == 2):
            if self._jan_open_price == 0:
                return
            jan_ret = (candle.ClosePrice - self._jan_open_price) / self._jan_open_price
            self.Rebalance(jan_ret > 0)

    def Rebalance(self, bullish):
        if bullish:
            self.Move(self.EquityETF, 1.0)
            self.Move(self.CashETF, 0.0)
        else:
            self.Move(self.EquityETF, 0.0)
            self.Move(self.CashETF, 1.0)

    def Move(self, security, weight):
        portfolio_value = self.Portfolio.CurrentValue or 0
        price = self.GetLatestPrice(security)
        if price <= 0:
            return
        tgt = weight * portfolio_value / price
        diff = tgt - self.PositionBy(security)
        if Math.Abs(diff) * price < self.MinTradeUsd:
            return
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "JanBar"
        self.RegisterOrder(order)

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def CreateClone(self):
        return january_barometer_strategy()
