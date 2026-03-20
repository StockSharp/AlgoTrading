import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class cryptos_strategy(Strategy):
    def __init__(self):
        super(cryptos_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._wma_period = self.Param("WmaPeriod", 55) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._bollinger_width = self.Param("BollingerWidth", 2) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")

        self._band_ema = None
        self._trend_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cryptos_strategy, self).OnReseted()
        self._band_ema = None
        self._trend_ema = None

    def OnStarted(self, time):
        super(cryptos_strategy, self).OnStarted(time)

        self.__band_ema = ExponentialMovingAverage()
        self.__band_ema.Length = self.bollinger_period
        self.__trend_ema = ExponentialMovingAverage()
        self.__trend_ema.Length = self.wma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__band_ema, self.__trend_ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return cryptos_strategy()
