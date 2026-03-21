import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bw_wiseman1_strategy(Strategy):
    def __init__(self):
        super(bw_wiseman1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._jaw_period = self.Param("JawPeriod", 34) \
            .SetDisplay("Jaw Period", "Slow EMA (Jaw)", "Indicators")
        self._teeth_period = self.Param("TeethPeriod", 13) \
            .SetDisplay("Teeth Period", "Medium EMA (Teeth)", "Indicators")
        self._lips_period = self.Param("LipsPeriod", 5) \
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
        super(bw_wiseman1_strategy, self).OnReseted()
        self._prev_lips = None
        self._prev_teeth = None

    def OnStarted(self, time):
        super(bw_wiseman1_strategy, self).OnStarted(time)
        self._prev_lips = None
        self._prev_teeth = None

        jaw = SmoothedMovingAverage()
        jaw.Length = self.JawPeriod
        teeth = SmoothedMovingAverage()
        teeth.Length = self.TeethPeriod
        lips = SmoothedMovingAverage()
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
        lv = float(lips_value)
        tv = float(teeth_value)
        if self._prev_lips is None or self._prev_teeth is None:
            self._prev_lips = lv
            self._prev_teeth = tv
            return
        # Lips crosses above teeth
        if self._prev_lips <= self._prev_teeth and lv > tv:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Lips crosses below teeth
        elif self._prev_lips >= self._prev_teeth and lv < tv:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._prev_lips = lv
        self._prev_teeth = tv

    def CreateClone(self):
        return bw_wiseman1_strategy()
