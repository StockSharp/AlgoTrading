import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from datatype_extensions import *

class bitcoin_intraday_seasonality_strategy(Strategy):
    """
    Strategy that goes long on Bitcoin during predefined strong intraday hours.
    Maintains a long position during specified UTC hours and exits otherwise.
    Skips trades smaller than the minimum USD value.
    """

    def __init__(self):
        super(bitcoin_intraday_seasonality_strategy, self).__init__()

        # Hours to stay long (UTC)
        self._hours_long = self.Param("HoursLong", [0, 1, 2, 3]) \
            .SetDisplay("Long Hours", "UTC hours when the strategy stays long", "General")

        # Minimum trade size in USD
        self._min_trade_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum order value in USD", "Trading")

        # Candle type used for processing
        self._candle_type = self.Param("CandleType", tf(60)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._latest_prices = {}

    @property
    def hours_long(self):
        """UTC hours when the strategy holds a long position."""
        return self._hours_long.Value

    @hours_long.setter
    def hours_long(self, value):
        self._hours_long.Value = value

    @property
    def min_trade_usd(self):
        """Minimum trade size in USD."""
        return self._min_trade_usd.Value

    @min_trade_usd.setter
    def min_trade_usd(self, value):
        self._min_trade_usd.Value = value

    @property
    def candle_type(self):
        """The type of candles to use for strategy calculation."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        if self.Security is None:
            raise Exception("BTC security not set.")
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(bitcoin_intraday_seasonality_strategy, self).OnReseted()
        self._latest_prices.clear()

    def OnStarted(self, time):
        if self.hours_long is None or len(self.hours_long) == 0:
            raise Exception("HoursLong cannot be empty.")
        if self.Security is None:
            raise Exception("BTC security not set.")

        super(bitcoin_intraday_seasonality_strategy, self).OnStarted(time)

        self.SubscribeCandles(self.candle_type, True, self.Security) \
            .Bind(lambda candle: self.ProcessCandle(candle, self.Security)) \
            .Start()

    def ProcessCandle(self, candle, security):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Store the latest closing price for this security
        self._latest_prices[security] = candle.ClosePrice

        self._on_hour_close(candle, security)

    def _on_hour_close(self, candle, security):
        hour = candle.OpenTime.UtcDateTime.Hour
        in_season = hour in self.hours_long

        portfolio_value = self.Portfolio.CurrentValue or 0.0
        price = self._latest_prices.get(security, 0.0)

        tgt = portfolio_value / price if in_season and price > 0 else 0.0
        diff = tgt - self._position_by(security)

        if price <= 0 or Math.Abs(diff) * price < self.min_trade_usd:
            return

        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "BTCSeason"
        self.RegisterOrder(order)

    def _position_by(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def CreateClone(self):
        """Creates a new instance of the strategy."""
        return bitcoin_intraday_seasonality_strategy()
