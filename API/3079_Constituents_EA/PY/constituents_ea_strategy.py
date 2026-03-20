import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class constituents_ea_strategy(Strategy):
    def __init__(self):
        super(constituents_ea_strategy, self).__init__()

        self._search_depth = self.Param("SearchDepth", 3) \
            .SetDisplay("Search Depth", "Number of completed candles used to find extremes", "Setup")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Search Depth", "Number of completed candles used to find extremes", "Setup")
        self._take_profit_pips = self.Param("TakeProfitPips", 100) \
            .SetDisplay("Search Depth", "Number of completed candles used to find extremes", "Setup")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Search Depth", "Number of completed candles used to find extremes", "Setup")

        self._highest = null!
        self._lowest = null!
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._exit_requested = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(constituents_ea_strategy, self).OnReseted()
        self._highest = null!
        self._lowest = null!
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._exit_requested = False

    def OnStarted(self, time):
        super(constituents_ea_strategy, self).OnStarted(time)

        self.__highest = Highest()
        self.__highest.Length = self.search_depth
        self.__lowest = Lowest()
        self.__lowest.Length = self.search_depth

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
        return constituents_ea_strategy()
