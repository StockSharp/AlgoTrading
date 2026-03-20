import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ten_kijun_cross_strategy(Strategy):
    def __init__(self):
        super(ten_kijun_cross_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for Ichimoku calculations", "General")
        self._tenkan_period = self.Param("TenkanPeriod", 12) \
            .SetDisplay("Candle Type", "Timeframe for Ichimoku calculations", "General")
        self._kijun_period = self.Param("KijunPeriod", 34) \
            .SetDisplay("Candle Type", "Timeframe for Ichimoku calculations", "General")

        self._highs_tenkan = new()
        self._lows_tenkan = new()
        self._highs_kijun = new()
        self._lows_kijun = new()
        self._prev_tenkan = None
        self._prev_kijun = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ten_kijun_cross_strategy, self).OnReseted()
        self._highs_tenkan = new()
        self._lows_tenkan = new()
        self._highs_kijun = new()
        self._lows_kijun = new()
        self._prev_tenkan = None
        self._prev_kijun = None

    def OnStarted(self, time):
        super(ten_kijun_cross_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ten_kijun_cross_strategy()
