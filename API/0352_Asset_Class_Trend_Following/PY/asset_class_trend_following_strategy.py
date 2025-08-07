import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *

class asset_class_trend_following_strategy(Strategy):
    """Asset class trend following strategy.
    Uses a simple moving average filter and rebalances monthly.
    """

    def __init__(self):
        super(asset_class_trend_following_strategy, self).__init__()

        self._universe = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._sma_len = self.Param("SmaLength", 210) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length", "Period of the SMA filter", "General")

        self._min_usd = self.Param("MinTradeUsd", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimal dollar amount per trade", "General")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._sma = {}
        self._sma_values = {}
        self._latest_prices = {}
        self._held = set()
        self._last_day = DateTime.MinValue

    # region Properties
    @property
    def Universe(self):
        """Trading universe."""
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def SmaLength(self):
        """SMA filter period."""
        return self._sma_len.Value

    @SmaLength.setter
    def SmaLength(self, value):
        self._sma_len.Value = value

    @property
    def MinTradeUsd(self):
        """Minimum dollar amount per trade."""
        return self._min_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_usd.Value = value

    @property
    def CandleType(self):
        """The type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value
    # endregion

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe]

    def OnReseted(self):
        super(asset_class_trend_following_strategy, self).OnReseted()
        self._sma.clear()
        self._sma_values.clear()
        self._latest_prices.clear()
        self._held.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty.")

        super(asset_class_trend_following_strategy, self).OnStarted(time)

        for sec, dt in self.GetWorkingSecurities():
            sma = SimpleMovingAverage()
            sma.Length = self.SmaLength
            self._sma[sec] = sma

            self.SubscribeCandles(dt, True, sec) \
                .Bind(sma, lambda candle, sma_val, s=sec: self.ProcessCandle(candle, s, sma_val)) \
                .Start()

    def ProcessCandle(self, candle, security, sma_value):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice

        self._sma_values[security] = float(sma_value) if hasattr(sma_value, '__float__') else float(sma_value)

        d = candle.OpenTime.Date
        if d == self._last_day:
            return
        self._last_day = d

        if d.Day == 1:
            self.TryRebalance()

    def TryRebalance(self):
        longs = []
        for sec, sma in self._sma.items():
            if sma.IsFormed:
                price = self.GetLatestPrice(sec)
                sma_val = self._sma_values.get(sec, 0)
                if price > 0 and price > sma_val:
                    longs.append(sec)

        for sec in list(self._held):
            if sec not in longs:
                self.Move(sec, 0)

        if len(longs) > 0:
            portfolio_value = self.Portfolio.CurrentValue or 0
            cap = portfolio_value / len(longs)
            for sec in longs:
                price = self.GetLatestPrice(sec)
                if price > 0:
                    self.Move(sec, cap / price)

        self._held.clear()
        self._held.update(longs)

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def Move(self, sec, tgt):
        diff = tgt - self.PositionBy(sec)
        price = self.GetLatestPrice(sec)
        if price <= 0 or Math.Abs(diff) * price < self.MinTradeUsd:
            return

        order = Order()
        order.Security = sec
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "ACTrend"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return asset_class_trend_following_strategy()
