import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class alligator_volatility_strategy(Strategy):
    """
    Alligator volatility strategy using three SMAs (Jaw, Teeth, Lips).
    Enters long when Lips > Teeth > Jaw (uptrend expansion), short when reversed.
    Exits when the lines converge (Alligator sleeps).
    """

    def __init__(self):
        super(alligator_volatility_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")

        self._jaw_period = self.Param("JawPeriod", 13) \
            .SetDisplay("Jaw Period", "Alligator jaw smoothing length.", "Indicators")

        self._teeth_period = self.Param("TeethPeriod", 8) \
            .SetDisplay("Teeth Period", "Alligator teeth smoothing length.", "Indicators")

        self._lips_period = self.Param("LipsPeriod", 5) \
            .SetDisplay("Lips Period", "Alligator lips smoothing length.", "Indicators")

        self._candle_count = 0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def JawPeriod(self): return self._jaw_period.Value
    @JawPeriod.setter
    def JawPeriod(self, v): self._jaw_period.Value = v
    @property
    def TeethPeriod(self): return self._teeth_period.Value
    @TeethPeriod.setter
    def TeethPeriod(self, v): self._teeth_period.Value = v
    @property
    def LipsPeriod(self): return self._lips_period.Value
    @LipsPeriod.setter
    def LipsPeriod(self, v): self._lips_period.Value = v

    def OnReseted(self):
        super(alligator_volatility_strategy, self).OnReseted()
        self._candle_count = 0

    def OnStarted(self, time):
        super(alligator_volatility_strategy, self).OnStarted(time)

        self._candle_count = 0

        jaw = SimpleMovingAverage()
        jaw.Length = self.JawPeriod
        teeth = SimpleMovingAverage()
        teeth.Length = self.TeethPeriod
        lips = SimpleMovingAverage()
        lips.Length = self.LipsPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(jaw, teeth, lips, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jaw)
            self.DrawIndicator(area, teeth)
            self.DrawIndicator(area, lips)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, jaw_value, teeth_value, lips_value):
        if candle.State != CandleStates.Finished:
            return

        self._candle_count += 1
        if self._candle_count < self.JawPeriod + 2:
            return

        close = float(candle.ClosePrice)

        bullish = lips_value > teeth_value and teeth_value > jaw_value
        bearish = lips_value < teeth_value and teeth_value < jaw_value

        # Exit conditions: lines converge
        if self.Position > 0 and not bullish:
            self.SellMarket()
        elif self.Position < 0 and not bearish:
            self.BuyMarket()

        # Entry conditions
        if self.Position == 0:
            if bullish and close > lips_value:
                self.BuyMarket()
            elif bearish and close < lips_value:
                self.SellMarket()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return alligator_volatility_strategy()
