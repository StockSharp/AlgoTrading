import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class hpcs_inter7_strategy(Strategy):
    def __init__(self):
        super(hpcs_inter7_strategy, self).__init__()

        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetDisplay("Bollinger Length", "Number of candles included in the Bollinger Bands calculation", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2) \
            .SetDisplay("Bollinger Length", "Number of candles included in the Bollinger Bands calculation", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Bollinger Length", "Number of candles included in the Bollinger Bands calculation", "Indicators")
        self._band_percent = self.Param("BandPercent", 0.01) \
            .SetDisplay("Bollinger Length", "Number of candles included in the Bollinger Bands calculation", "Indicators")

        self._prev_close = None
        self._prev_lower = None
        self._prev_upper = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hpcs_inter7_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_lower = None
        self._prev_upper = None

    def OnStarted(self, time):
        super(hpcs_inter7_strategy, self).OnStarted(time)

        self._bollinger = ExponentialMovingAverage()
        self._bollinger.Length = self.bollinger_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._bollinger, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return hpcs_inter7_strategy()
