import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_band_pending_stops_strategy(Strategy):
    def __init__(self):
        super(bollinger_band_pending_stops_strategy, self).__init__()

        self._band_period = self.Param("BandPeriod", 20) \
            .SetDisplay("Band Period", "Bollinger bands period", "Indicators")
        self._band_width = self.Param("BandWidth", 1) \
            .SetDisplay("Band Period", "Bollinger bands period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Band Period", "Bollinger bands period", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_band_pending_stops_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(bollinger_band_pending_stops_strategy, self).OnStarted(time)

        self._bb = BollingerBands()
        self._bb.Length = self.band_period
        self._bb.Width = self.band_width

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bollinger_band_pending_stops_strategy()
