import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import FractalAdaptiveMovingAverage
from StockSharp.Algo.Strategies import Strategy


class frama_candle_trend_strategy(Strategy):
    def __init__(self):
        super(frama_candle_trend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for indicator calculation", "General")
        self._frama_period = self.Param("FramaPeriod", 15) \
            .SetDisplay("FrAMA Period", "Length of the Fractal Adaptive Moving Average", "Indicator")
        self._prev_frama_value = 0.0
        self._prev_prev_frama_value = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def frama_period(self):
        return self._frama_period.Value

    def OnReseted(self):
        super(frama_candle_trend_strategy, self).OnReseted()
        self._prev_frama_value = 0.0
        self._prev_prev_frama_value = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(frama_candle_trend_strategy, self).OnStarted2(time)
        self._has_prev = False
        frama = FractalAdaptiveMovingAverage()
        frama.Length = self.frama_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(frama, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, frama)
            self.DrawOwnTrades(area)

    def on_process(self, candle, frama_value):
        if candle.State != CandleStates.Finished:
            return
        frama_value = float(frama_value)
        if not self._has_prev:
            self._prev_frama_value = frama_value
            self._has_prev = True
            return

        rising = frama_value > self._prev_frama_value
        falling = frama_value < self._prev_frama_value
        was_rising = self._prev_frama_value > self._prev_prev_frama_value
        was_falling = self._prev_frama_value < self._prev_prev_frama_value

        if rising and was_falling and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif falling and was_rising and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_frama_value = self._prev_frama_value
        self._prev_frama_value = frama_value

    def CreateClone(self):
        return frama_candle_trend_strategy()
