import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class night_flat_trade_strategy(Strategy):
    def __init__(self):
        super(night_flat_trade_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Type of candles used for the setup", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Candle Type", "Type of candles used for the setup", "General")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 15) \
            .SetDisplay("Candle Type", "Type of candles used for the setup", "General")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Candle Type", "Type of candles used for the setup", "General")
        self._diff_min_pips = self.Param("DiffMinPips", 18) \
            .SetDisplay("Candle Type", "Type of candles used for the setup", "General")
        self._diff_max_pips = self.Param("DiffMaxPips", 28) \
            .SetDisplay("Candle Type", "Type of candles used for the setup", "General")
        self._open_hour = self.Param("OpenHour", 0) \
            .SetDisplay("Candle Type", "Type of candles used for the setup", "General")
        self._range_length = self.Param("RangeLength", 3) \
            .SetDisplay("Candle Type", "Type of candles used for the setup", "General")

        self._highest = null!
        self._lowest = null!
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_profit_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(night_flat_trade_strategy, self).OnReseted()
        self._highest = null!
        self._lowest = null!
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_profit_price = None

    def OnStarted(self, time):
        super(night_flat_trade_strategy, self).OnStarted(time)

        self.__highest = Highest()
        self.__highest.Length = self.range_length
        self.__lowest = Lowest()
        self.__lowest.Length = self.range_length

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
        return night_flat_trade_strategy()
