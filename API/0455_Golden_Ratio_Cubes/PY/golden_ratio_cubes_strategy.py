import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Messages import CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import Highest, Lowest
from datatype_extensions import *
from indicator_extensions import *

class golden_ratio_cubes_strategy(Strategy):
    """Golden Ratio breakout strategy.

    Uses highest and lowest over a lookback period to build a range and
    trades when price breaks golden ratio extensions of that range.
    """

    def __init__(self):
        super(golden_ratio_cubes_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._lookback = self.Param("Lookback", 34) \
            .SetDisplay("Lookback", "Lookback period for highest and lowest", "Golden Ratio")
        self._phi = self.Param("Phi", 1.618) \
            .SetDisplay("Phi", "Golden ratio multiplier", "Golden Ratio")

        self._highest = None
        self._lowest = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def lookback(self):
        return self._lookback.Value

    @lookback.setter
    def lookback(self, value):
        self._lookback.Value = value

    @property
    def phi(self):
        return self._phi.Value

    @phi.setter
    def phi(self, value):
        self._phi.Value = value

    def OnStarted(self, time):
        super(golden_ratio_cubes_strategy, self).OnStarted(time)

        self._highest = Highest()
        self._highest.Length = self.lookback

        self._lowest = Lowest()
        self._lowest.Length = self.lookback

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._highest, self._lowest, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        self.StartProtection()

    def ProcessCandle(self, candle, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        high = float(highest_value)
        low = float(lowest_value)
        rng = high - low

        upper = high + rng / self.phi
        lower = low - rng / self.phi
        price = candle.ClosePrice

        if price > upper and self.Position <= 0:
            self.BuyMarket()
        elif price < lower and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return golden_ratio_cubes_strategy()
