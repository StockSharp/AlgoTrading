import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *

class crypto_rebalancing_premium_strategy(Strategy):
    """Equal-weight BTC and ETH basket rebalancing strategy."""

    def __init__(self):
        super(crypto_rebalancing_premium_strategy, self).__init__()

        self._eth = self.Param("ETH", None) \
            .SetDisplay("ETH", "Ethereum security", "General")

        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum dollar amount per trade", "Trading")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._latest_prices = {}
        self._last = DateTime.MinValue

    # region Properties
    @property
    def BTC(self):
        return self.Security

    @BTC.setter
    def BTC(self, value):
        self.Security = value

    @property
    def ETH(self):
        return self._eth.Value

    @ETH.setter
    def ETH(self, value):
        self._eth.Value = value

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
        return [(self.BTC, self.CandleType), (self.ETH, self.CandleType)]

    def OnReseted(self):
        super(crypto_rebalancing_premium_strategy, self).OnReseted()
        self._latest_prices.clear()
        self._last = DateTime.MinValue

    def OnStarted(self, time):
        super(crypto_rebalancing_premium_strategy, self).OnStarted(time)

        for sec, dt in self.GetWorkingSecurities():
            if sec is None:
                raise Exception("Working securities collection is empty or contains null.")
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice
        self.OnTick(candle.OpenTime.UtcDateTime)

    def OnTick(self, utc):
        if utc == self._last:
            return
        self._last = utc
        if utc.DayOfWeek != 0 or utc.Hour != 0:  # Monday 00:00
            return
        self.Rebalance()

    def Rebalance(self):
        portfolio_value = self.Portfolio.CurrentValue or 0
        half = portfolio_value / 2
        btc_price = self.GetLatestPrice(self.BTC)
        eth_price = self.GetLatestPrice(self.ETH)
        if btc_price > 0:
            self.Move(self.BTC, half / btc_price)
        if eth_price > 0:
            self.Move(self.ETH, half / eth_price)

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def Move(self, security, target):
        diff = target - self.PositionBy(security)
        price = self.GetLatestPrice(security)
        if price <= 0 or Math.Abs(diff) * price < self.MinTradeUsd:
            return
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "RebalPrem"
        self.RegisterOrder(order)

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def CreateClone(self):
        return crypto_rebalancing_premium_strategy()
