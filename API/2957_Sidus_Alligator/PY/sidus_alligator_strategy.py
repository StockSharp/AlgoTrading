import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class sidus_alligator_strategy(Strategy):
    def __init__(self):
        super(sidus_alligator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._jaw_period = self.Param("JawPeriod", 50) \
            .SetDisplay("Jaw Period", "Slow EMA (Jaw)", "Indicators")
        self._teeth_period = self.Param("TeethPeriod", 25) \
            .SetDisplay("Teeth Period", "Medium EMA (Teeth)", "Indicators")
        self._lips_period = self.Param("LipsPeriod", 10) \
            .SetDisplay("Lips Period", "Fast EMA (Lips)", "Indicators")

        self._prev_lips = None
        self._prev_teeth = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def JawPeriod(self):
        return self._jaw_period.Value

    @property
    def TeethPeriod(self):
        return self._teeth_period.Value

    @property
    def LipsPeriod(self):
        return self._lips_period.Value

    def OnReseted(self):
        super(sidus_alligator_strategy, self).OnReseted()
        self._prev_lips = None
        self._prev_teeth = None

    def OnStarted(self, time):
        super(sidus_alligator_strategy, self).OnStarted(time)
        self._prev_lips = None
        self._prev_teeth = None

        jaw = ExponentialMovingAverage()
        jaw.Length = self.JawPeriod
        teeth = ExponentialMovingAverage()
        teeth.Length = self.TeethPeriod
        lips = ExponentialMovingAverage()
        lips.Length = self.LipsPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(jaw, teeth, lips, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jaw)
            self.DrawIndicator(area, teeth)
            self.DrawIndicator(area, lips)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, jaw_value, teeth_value, lips_value):
        if candle.State != CandleStates.Finished:
            return

        jv = float(jaw_value)
        tv = float(teeth_value)
        lv = float(lips_value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_lips = lv
            self._prev_teeth = tv
            return

        if self._prev_lips is None or self._prev_teeth is None:
            self._prev_lips = lv
            self._prev_teeth = tv
            return

        # Lips crosses above teeth with alligator aligned (lips > teeth > jaw)
        if self._prev_lips <= self._prev_teeth and lv > tv and lv > jv:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Lips crosses below teeth with alligator aligned (lips < teeth < jaw)
        elif self._prev_lips >= self._prev_teeth and lv < tv and lv < jv:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_lips = lv
        self._prev_teeth = tv

    def CreateClone(self):
        return sidus_alligator_strategy()
