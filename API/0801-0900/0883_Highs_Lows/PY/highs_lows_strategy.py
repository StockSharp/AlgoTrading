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

class highs_lows_strategy(Strategy):
    """
    Highs Lows strategy.
    Tracks the highest high and lowest low of the last Range candles. The candle midpoint is compared with the average of
    these extremes, and their distance is normalized by half the range (0 at the average, 100 at an extreme). A long opens when the midpoint is
    below the average and the normalized distance is below LowThreshold, and closes when the midpoint is above the average
    and the normalized distance is above HighThreshold.
    """

    def __init__(self):
        super(highs_lows_strategy, self).__init__()
        self._range = self.Param("Range", 100).SetGreaterThanZero().SetDisplay("Range", "Number of candles for highest and lowest values", "Indicators")
        self._low_threshold = self.Param("LowThreshold", 15.0).SetDisplay("Low Threshold", "Normalized distance below which a long opens", "Signals")
        self._high_threshold = self.Param("HighThreshold", 85.0).SetDisplay("High Threshold", "Normalized distance above which the long closes", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(240))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._highest = None
        self._lowest = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(highs_lows_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None

    def OnStarted2(self, time):
        super(highs_lows_strategy, self).OnStarted2(time)

        self._highest = Highest()
        self._highest.Length = self._range.Value
        self._lowest = Lowest()
        self._lowest.Length = self._range.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._highest)
            self.DrawIndicator(area, self._lowest)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        highest = process_float(self._highest, candle.HighPrice, candle.OpenTime, True).GetValue[Decimal](None)
        lowest = process_float(self._lowest, candle.LowPrice, candle.OpenTime, True).GetValue[Decimal](None)

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        width = highest - lowest
        if width <= 0:
            return

        midpoint = (candle.HighPrice + candle.LowPrice) / Decimal(2)
        average = (highest + lowest) / Decimal(2)
        # Distance from the average as a percentage of the half range: 0 at the average, 100 at an extreme.
        distance = abs(midpoint - average) / (width / Decimal(2)) * Decimal(100)

        if self.Position <= 0 and midpoint < average and distance < Decimal(self._low_threshold.Value):
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and midpoint > average and distance > Decimal(self._high_threshold.Value):
            self.SellMarket(self.Position)

    def CreateClone(self):
        return highs_lows_strategy()
