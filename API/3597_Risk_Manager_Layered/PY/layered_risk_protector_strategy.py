import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class layered_risk_protector_strategy(Strategy):
    def __init__(self):
        super(layered_risk_protector_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Primary candle series", "General")
        self._cci_length = self.Param("CciLength", 100) \
            .SetDisplay("Candle Type", "Primary candle series", "General")
        self._cci_level = self.Param("CciLevel", 150) \
            .SetDisplay("Candle Type", "Primary candle series", "General")

        self._cci = None
        self._prev_cci = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(layered_risk_protector_strategy, self).OnReseted()
        self._cci = None
        self._prev_cci = None

    def OnStarted(self, time):
        super(layered_risk_protector_strategy, self).OnStarted(time)

        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.cci_length

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
        return layered_risk_protector_strategy()
