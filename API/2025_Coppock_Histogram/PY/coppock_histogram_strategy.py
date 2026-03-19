import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class coppock_histogram_strategy(Strategy):
    """
    Strategy based on Coppock histogram turns to capture trend reversals.
    """

    def __init__(self):
        super(coppock_histogram_strategy, self).__init__()
        self._roc1_period = self.Param("Roc1Period", 14) \
            .SetDisplay("ROC1 Period", "First ROC length", "Parameters")
        self._roc2_period = self.Param("Roc2Period", 11) \
            .SetDisplay("ROC2 Period", "Second ROC length", "Parameters")
        self._smooth_period = self.Param("SmoothPeriod", 3) \
            .SetDisplay("Smoothing", "Moving average length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 2) \
            .SetDisplay("Signal Cooldown", "Closed candles to wait before the next trade", "Parameters")

        self._sma = None
        self._prev = None
        self._prev2 = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(coppock_histogram_strategy, self).OnReseted()
        self._prev = None
        self._prev2 = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(coppock_histogram_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self._smooth_period.Value

        roc1 = RateOfChange()
        roc1.Length = self._roc1_period.Value
        roc2 = RateOfChange()
        roc2.Length = self._roc2_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(roc1, roc2, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, roc1_val, roc2_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        smooth_val = self._sma.Process(roc1_val + roc2_val, candle.OpenTime, True)
        if not smooth_val.IsFinal or smooth_val.IsEmpty or not self._sma.IsFormed:
            return

        coppock = float(smooth_val)

        if self._prev is not None and self._prev2 is not None:
            if self._cooldown_remaining == 0 and self._prev < self._prev2 and coppock > self._prev and self.Position <= 0:
                self.BuyMarket()
                self._cooldown_remaining = self._signal_cooldown_bars.Value
            elif self._cooldown_remaining == 0 and self._prev > self._prev2 and coppock < self._prev and self.Position >= 0:
                self.SellMarket()
                self._cooldown_remaining = self._signal_cooldown_bars.Value

        self._prev2 = self._prev
        self._prev = coppock

    def CreateClone(self):
        return coppock_histogram_strategy()
