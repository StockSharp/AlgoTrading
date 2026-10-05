import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class golden_ratio_cubes_strategy(Strategy):
    """
    Golden Ratio Cubes Strategy.
    The range spans the highest high and lowest low of the previous Lookback candles. Its golden ratio extensions are
    lowest + Phi * range above and highest - Phi * range below. A close above the upper extension buys and a close below the
    lower extension sells; an opposite breakout reverses the position.
    """

    def __init__(self):
        super(golden_ratio_cubes_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 34).SetGreaterThanZero().SetDisplay("Lookback", "Candles the range spans", "Indicators")
        self._phi = self.Param("Phi", 1.618).SetGreaterThanZero().SetDisplay("Phi", "Golden ratio of the extensions", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._highest = None
        self._lowest = None
        self._prev_highest = None
        self._prev_lowest = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(golden_ratio_cubes_strategy, self).OnReseted()
        self._prev_highest = None
        self._prev_lowest = None

    def OnStarted2(self, time):
        super(golden_ratio_cubes_strategy, self).OnStarted2(time)

        self._prev_highest = None
        self._prev_lowest = None

        self._highest = Highest()
        self._highest.Length = self._lookback.Value
        self._lowest = Lowest()
        self._lowest.Length = self._lookback.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # The range is measured on the candles before this one.
        range_high = self._prev_highest
        range_low = self._prev_lowest

        highest = float(process_float(self._highest, candle.HighPrice, candle.OpenTime, True).GetValue[Decimal](None))
        lowest = float(process_float(self._lowest, candle.LowPrice, candle.OpenTime, True).GetValue[Decimal](None))

        if self._highest.IsFormed and self._lowest.IsFormed:
            self._prev_highest = highest
            self._prev_lowest = lowest

        if range_high is None or range_low is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        phi = float(self._phi.Value)
        rng = range_high - range_low
        upper_extension = range_low + phi * rng
        lower_extension = range_high - phi * rng
        close = float(candle.ClosePrice)

        if close > upper_extension and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < lower_extension and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return golden_ratio_cubes_strategy()
