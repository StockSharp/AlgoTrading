import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class cci_macd_scalper_strategy(Strategy):
    def __init__(self):
        super(cci_macd_scalper_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Timeframe for scalping", "General")
        self._ema_period = self.Param("EmaPeriod", 21) \
            .SetDisplay("Candle Type", "Timeframe for scalping", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe for scalping", "General")

        self._ema = None
        self._cci = None
        self._prev_cci = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_macd_scalper_strategy, self).OnReseted()
        self._ema = None
        self._cci = None
        self._prev_cci = None

    def OnStarted(self, time):
        super(cci_macd_scalper_strategy, self).OnStarted(time)

        self.__ema = ExponentialMovingAverage()
        self.__ema.Length = self.ema_period
        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__ema, self.__cci, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return cci_macd_scalper_strategy()
