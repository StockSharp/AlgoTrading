import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class zig_and_zag_trader_strategy(Strategy):
    def __init__(self):
        super(zig_and_zag_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(1) \
            .SetDisplay("Candle Type", "Candles used for swing detection", "General")
        self._lots = self.Param("Lots", 0.1) \
            .SetDisplay("Candle Type", "Candles used for swing detection", "General")
        self._trend_depth = self.Param("TrendDepth", 3) \
            .SetDisplay("Candle Type", "Candles used for swing detection", "General")
        self._exit_depth = self.Param("ExitDepth", 3) \
            .SetDisplay("Candle Type", "Candles used for swing detection", "General")
        self._max_orders = self.Param("MaxOrders", 1) \
            .SetDisplay("Candle Type", "Candles used for swing detection", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 0) \
            .SetDisplay("Candle Type", "Candles used for swing detection", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 0) \
            .SetDisplay("Candle Type", "Candles used for swing detection", "General")

        self._long_term_low = null!
        self._long_term_high = null!
        self._short_term_low = null!
        self._short_term_high = null!
        self._pip_size = 0.0
        self._volume_step = 0.0
        self._breakout_threshold = 0.0
        self._last_trend_low = None
        self._last_trend_high = None
        self._last_short_low = None
        self._last_short_high = None
        self._last_slalom_zig = None
        self._last_slalom_zag = None
        self._trend_up = False
        self._prev_trend_up = False
        self._buy_armed = False
        self._sell_armed = False
        self._limit_armed = False
        self._last_pivot = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zig_and_zag_trader_strategy, self).OnReseted()
        self._long_term_low = null!
        self._long_term_high = null!
        self._short_term_low = null!
        self._short_term_high = null!
        self._pip_size = 0.0
        self._volume_step = 0.0
        self._breakout_threshold = 0.0
        self._last_trend_low = None
        self._last_trend_high = None
        self._last_short_low = None
        self._last_short_high = None
        self._last_slalom_zig = None
        self._last_slalom_zag = None
        self._trend_up = False
        self._prev_trend_up = False
        self._buy_armed = False
        self._sell_armed = False
        self._limit_armed = False
        self._last_pivot = None

    def OnStarted(self, time):
        super(zig_and_zag_trader_strategy, self).OnStarted(time)

        self.__long_term_low = Lowest()
        self.__long_term_low.Length = self.trend_depth
        self.__long_term_high = Highest()
        self.__long_term_high.Length = self.trend_depth
        self.__short_term_low = Lowest()
        self.__short_term_low.Length = self.exit_depth
        self.__short_term_high = Highest()
        self.__short_term_high.Length = self.exit_depth

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
        return zig_and_zag_trader_strategy()
