import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class expert_alligator_strategy(Strategy):
    def __init__(self):
        super(expert_alligator_strategy, self).__init__()

        self._lips = None
        self._teeth = None
        self._jaw = None
        self._prev_lips = None
        self._prev_teeth = None

    def OnReseted(self):
        super(expert_alligator_strategy, self).OnReseted()
        self._lips = None
        self._teeth = None
        self._jaw = None
        self._prev_lips = None
        self._prev_teeth = None

    def OnStarted(self, time):
        super(expert_alligator_strategy, self).OnStarted(time)

        self._lips = SimpleMovingAverage()
        self._lips.Length = 5
        self._teeth = SimpleMovingAverage()
        self._teeth.Length = 8
        self._jaw = SimpleMovingAverage()
        self._jaw.Length = 13

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._lips, self._teeth, self._jaw, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, lips_value, teeth_value, jaw_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._lips.IsFormed or not self._teeth.IsFormed or not self._jaw.IsFormed:
            return

        lips_val = float(lips_value)
        teeth_val = float(teeth_value)
        jaw_val = float(jaw_value)

        if self._prev_lips is not None and self._prev_teeth is not None:
            lips_cross_up = self._prev_lips <= self._prev_teeth and lips_val > teeth_val
            lips_cross_down = self._prev_lips >= self._prev_teeth and lips_val < teeth_val

            if lips_cross_up and teeth_val > jaw_val and self.Position <= 0:
                self.BuyMarket()
            elif lips_cross_down and teeth_val < jaw_val and self.Position >= 0:
                self.SellMarket()

        self._prev_lips = lips_val
        self._prev_teeth = teeth_val

    def CreateClone(self):
        return expert_alligator_strategy()
