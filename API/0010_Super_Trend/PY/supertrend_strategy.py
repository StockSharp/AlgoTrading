import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class supertrend_strategy(Strategy):
    """
    Strategy based on Supertrend indicator.
    Enters long when price crosses above Supertrend, short when price crosses below.
    """

    def __init__(self):
        super(supertrend_strategy, self).__init__()
        self._period = self.Param("Period", 300).SetDisplay("Period", "Period for Supertrend calculation", "Indicators")
        self._multiplier = self.Param("Multiplier", 50.0).SetDisplay("Multiplier", "Multiplier for Supertrend calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_is_price_above = False
        self._prev_supertrend = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supertrend_strategy, self).OnReseted()
        self._prev_is_price_above = False
        self._prev_supertrend = 0.0

    def OnStarted(self, time):
        super(supertrend_strategy, self).OnStarted(time)

        atr = AverageTrueRange()
        atr.Length = self._period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr_v = float(atr_val)
        mult = float(self._multiplier.Value)
        median = float(candle.HighPrice + candle.LowPrice) / 2.0
        basic_upper = median + mult * atr_v
        basic_lower = median - mult * atr_v

        if self._prev_supertrend == 0:
            st = basic_lower if float(candle.ClosePrice) > median else basic_upper
            self._prev_supertrend = st
            self._prev_is_price_above = float(candle.ClosePrice) > st
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._prev_supertrend <= high:
            st = max(basic_lower, self._prev_supertrend)
        elif self._prev_supertrend >= low:
            st = min(basic_upper, self._prev_supertrend)
        else:
            st = basic_lower if close > self._prev_supertrend else basic_upper

        is_above = close > st
        crossed_above = is_above and not self._prev_is_price_above
        crossed_below = not is_above and self._prev_is_price_above

        if crossed_above and self.Position <= 0:
            self.BuyMarket()
        elif crossed_below and self.Position >= 0:
            self.SellMarket()

        self._prev_supertrend = st
        self._prev_is_price_above = is_above

    def CreateClone(self):
        return supertrend_strategy()
