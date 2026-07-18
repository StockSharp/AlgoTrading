import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hpcs_inter7_strategy(Strategy):
    def __init__(self):
        super(hpcs_inter7_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._bollinger_length = self.Param("BollingerLength", 20)
        self._band_percent = self.Param("BandPercent", 0.01)

        self._prev_close = 0.0
        self._prev_lower = 0.0
        self._prev_upper = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BollingerLength(self):
        return self._bollinger_length.Value

    @BollingerLength.setter
    def BollingerLength(self, value):
        self._bollinger_length.Value = value

    @property
    def BandPercent(self):
        return self._band_percent.Value

    @BandPercent.setter
    def BandPercent(self, value):
        self._band_percent.Value = value

    def OnReseted(self):
        super(hpcs_inter7_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_lower = 0.0
        self._prev_upper = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(hpcs_inter7_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_lower = 0.0
        self._prev_upper = 0.0
        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.BollingerLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

    def _process_candle(self, candle, middle_value):
        if candle.State != CandleStates.Finished:
            return

        middle = float(middle_value)
        band_pct = float(self.BandPercent)
        upper = middle * (1.0 + band_pct)
        lower = middle * (1.0 - band_pct)
        close = float(candle.ClosePrice)

        if self._has_prev:
            # Downward cross through lower band - short
            if self._prev_close > self._prev_lower and close < lower and self.Position >= 0:
                self.SellMarket()
            # Upward cross through upper band - long
            elif self._prev_close < self._prev_upper and close > upper and self.Position <= 0:
                self.BuyMarket()

        self._prev_close = close
        self._prev_lower = lower
        self._prev_upper = upper
        self._has_prev = True

    def CreateClone(self):
        return hpcs_inter7_strategy()
