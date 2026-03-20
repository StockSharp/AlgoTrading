import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class zig_zag_evge_trofi_strategy(Strategy):
    def __init__(self):
        super(zig_zag_evge_trofi_strategy, self).__init__()

        self._depth = self.Param("Depth", 17) \
            .SetDisplay("Depth", "ZigZag depth parameter", "ZigZag")
        self._deviation = self.Param("Deviation", 7) \
            .SetDisplay("Depth", "ZigZag depth parameter", "ZigZag")
        self._backstep = self.Param("Backstep", 5) \
            .SetDisplay("Depth", "ZigZag depth parameter", "ZigZag")
        self._urgency = self.Param("Urgency", 2) \
            .SetDisplay("Depth", "ZigZag depth parameter", "ZigZag")
        self._signal_reverse = self.Param("SignalReverse", False) \
            .SetDisplay("Depth", "ZigZag depth parameter", "ZigZag")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Depth", "ZigZag depth parameter", "ZigZag")
        self._volume = self.Param("VolumePerTrade", 0.1) \
            .SetDisplay("Depth", "ZigZag depth parameter", "ZigZag")

        self._highest = None
        self._lowest = None
        self._pivot_type = None
        self._pivot_price = 0.0
        self._bars_since_pivot = 0.0
        self._price_step = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zig_zag_evge_trofi_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._pivot_type = None
        self._pivot_price = 0.0
        self._bars_since_pivot = 0.0
        self._price_step = 0.0

    def OnStarted(self, time):
        super(zig_zag_evge_trofi_strategy, self).OnStarted(time)

        self.__highest = Highest()
        self.__highest.Length = self.depth
        self.__lowest = Lowest()
        self.__lowest.Length = self.depth

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__highest, self.__lowest, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return zig_zag_evge_trofi_strategy()
