import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class cho_smoothed_ea_strategy(Strategy):
    def __init__(self):
        super(cho_smoothed_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for signal calculations", "General")
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe for signal calculations", "General")
        self._ma_period = self.Param("MaPeriod", 9) \
            .SetDisplay("Candle Type", "Timeframe for signal calculations", "General")

        self._cci = None
        self._cci_history = new()
        self._prev_cci = None
        self._prev_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cho_smoothed_ea_strategy, self).OnReseted()
        self._cci = None
        self._cci_history = new()
        self._prev_cci = None
        self._prev_signal = None

    def OnStarted(self, time):
        super(cho_smoothed_ea_strategy, self).OnStarted(time)

        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__cci, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return cho_smoothed_ea_strategy()
