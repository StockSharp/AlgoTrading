import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bb_strategy(Strategy):
    def __init__(self):
        super(bb_strategy, self).__init__()

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._inner_deviation = self.Param("InnerDeviation", 2) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._outer_deviation = self.Param("OuterDeviation", 3) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bb_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(bb_strategy, self).OnStarted(time)

        self._inner_band = BollingerBands()
        self._inner_band.Length = self.bollinger_period
        self._inner_band.Width = self.inner_deviation
        self._outer_band = BollingerBands()
        self._outer_band.Length = self.bollinger_period
        self._outer_band.Width = self.outer_deviation

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bb_strategy()
