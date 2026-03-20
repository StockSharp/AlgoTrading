import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class aussie_surfer_ltd_strategy(Strategy):
    def __init__(self):
        super(aussie_surfer_ltd_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(120) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._bollinger_width = self.Param("BollingerWidth", 2.5) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._sma_period = self.Param("SmaPeriod", 21) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")

        self._band_ema = None
        self._slope_ema = None
        self._prev_sma = None
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(aussie_surfer_ltd_strategy, self).OnReseted()
        self._band_ema = None
        self._slope_ema = None
        self._prev_sma = None
        self._prev_close = None

    def OnStarted(self, time):
        super(aussie_surfer_ltd_strategy, self).OnStarted(time)

        self.__band_ema = ExponentialMovingAverage()
        self.__band_ema.Length = self.bollinger_period
        self.__slope_ema = ExponentialMovingAverage()
        self.__slope_ema.Length = self.sma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__band_ema, self.__slope_ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return aussie_surfer_ltd_strategy()
