import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class rsi_alert_strategy(Strategy):
    def __init__(self):
        super(rsi_alert_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.01) \
            .SetDisplay("Order Volume", "Order size used for market trades", "Trading")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Order Volume", "Order size used for market trades", "Trading")
        self._overbought_level = self.Param("OverboughtLevel", 70) \
            .SetDisplay("Order Volume", "Order size used for market trades", "Trading")
        self._oversold_level = self.Param("OversoldLevel", 30) \
            .SetDisplay("Order Volume", "Order size used for market trades", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Order Volume", "Order size used for market trades", "Trading")

        self._rsi = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_alert_strategy, self).OnReseted()
        self._rsi = None

    def OnStarted(self, time):
        super(rsi_alert_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rsi_alert_strategy()
