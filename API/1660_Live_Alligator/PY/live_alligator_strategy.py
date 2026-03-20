import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class live_alligator_strategy(Strategy):
    def __init__(self):
        super(live_alligator_strategy, self).__init__()
        self._jaw_length = self.Param("JawLength", 21) \
            .SetDisplay("Jaw", "Alligator Jaw length", "Indicators")
        self._teeth_length = self.Param("TeethLength", 13) \
            .SetDisplay("Teeth", "Alligator Teeth length", "Indicators")
        self._lips_length = self.Param("LipsLength", 8) \
            .SetDisplay("Lips", "Alligator Lips length", "Indicators")
        self._trail_length = self.Param("TrailLength", TimeSpan.FromHours(4)) \
            .SetDisplay("Trail", "Trailing SMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_lips = 0.0
        self._prev_jaw = 0.0
        self._prev_trail = 0.0
        self._has_prev = False

    @property
    def jaw_length(self):
        return self._jaw_length.Value

    @property
    def teeth_length(self):
        return self._teeth_length.Value

    @property
    def lips_length(self):
        return self._lips_length.Value

    @property
    def trail_length(self):
        return self._trail_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(live_alligator_strategy, self).OnReseted()
        self._prev_lips = 0.0
        self._prev_jaw = 0.0
        self._prev_trail = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(live_alligator_strategy, self).OnStarted(time)
        jaw = SmoothedMovingAverage()
        jaw.Length = self.jaw_length
        teeth = SmoothedMovingAverage()
        teeth.Length = self.teeth_length
        lips = SmoothedMovingAverage()
        lips.Length = self.lips_length
        trail = SimpleMovingAverage()
        trail.Length = self.trail_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jaw, teeth, lips, trail, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, jaw_val, teeth_val, lips_val, trail_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_lips = lips_val
            self._prev_jaw = jaw_val
            self._prev_trail = trail_val
            self._has_prev = True
            return
        close = candle.ClosePrice
        # Lips cross above jaw -> uptrend start
        if self._prev_lips <= self._prev_jaw and lips_val > jaw_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Lips cross below jaw -> downtrend start
        elif self._prev_lips >= self._prev_jaw and lips_val < jaw_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Trail exit: close below trail for longs
        if self.Position > 0 and close < self._prev_trail:
            self.SellMarket()
        # Trail exit: close above trail for shorts
        elif self.Position < 0 and close > self._prev_trail:
            self.BuyMarket()
        self._prev_lips = lips_val
        self._prev_jaw = jaw_val
        self._prev_trail = trail_val

    def CreateClone(self):
        return live_alligator_strategy()
