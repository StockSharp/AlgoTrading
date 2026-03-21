import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AwesomeOscillator
from StockSharp.Algo.Strategies import Strategy


class ao_lightning_strategy(Strategy):
    def __init__(self):
        super(ao_lightning_strategy, self).__init__()

        self._ao_short_period = self.Param("AoShortPeriod", 5) \
            .SetDisplay("AO Fast", "Short SMA period for Awesome Oscillator", "Indicators")
        self._ao_long_period = self.Param("AoLongPeriod", 34) \
            .SetDisplay("AO Slow", "Long SMA period for Awesome Oscillator", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source candles", "General")

        self._prev_ao = 0.0
        self._initialized = False

    @property
    def AoShortPeriod(self):
        return self._ao_short_period.Value

    @property
    def AoLongPeriod(self):
        return self._ao_long_period.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ao_lightning_strategy, self).OnReseted()
        self._prev_ao = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(ao_lightning_strategy, self).OnStarted(time)

        self._prev_ao = 0.0
        self._initialized = False

        ao = AwesomeOscillator()
        ao.ShortMa.Length = self.AoShortPeriod
        ao.LongMa.Length = self.AoLongPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(ao, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ao)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ao_value):
        if candle.State != CandleStates.Finished:
            return

        av = float(ao_value)

        if not self._initialized:
            self._prev_ao = av
            self._initialized = True
            return

        if av > self._prev_ao and self.Position <= 0:
            self.BuyMarket()
        elif av < self._prev_ao and self.Position >= 0:
            self.SellMarket()

        self._prev_ao = av

    def CreateClone(self):
        return ao_lightning_strategy()
