import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage, SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides
from StockSharp.Messages import Unit, UnitTypes


class expert_zzlwa_strategy(Strategy):
    def __init__(self):
        super(expert_zzlwa_strategy, self).__init__()

        self._stop_loss_points = self.Param("StopLossPoints", 600) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 700) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._base_volume = self.Param("BaseVolume", 0.01) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._use_martingale = self.Param("UseMartingale", False) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._martingale_multiplier = self.Param("MartingaleMultiplier", 2) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._maximum_volume = self.Param("MaximumVolume", 10) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._mode = self.Param("Mode", StrategyModes.MovingAverageTest) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._term_level = self.Param("ZigZagTerm", TermLevels.LongTer) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._slow_ma_period = self.Param("SlowMaPeriod", 150) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._fast_ma_period = self.Param("FastMaPeriod", 10) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")

        self._highest = None
        self._lowest = None
        self._slow_ma = None
        self._fast_ma = None
        self._pending_buy_signal = False
        self._pending_sell_signal = False
        self._original_buy_ready = False
        self._original_sell_ready = False
        self._zig_zag_direction = 0.0
        self._prev_slow = 0.0
        self._prev_fast = 0.0
        self._tracked_position = 0.0
        self._average_entry_price = 0.0
        self._last_closed_volume = 0.0
        self._last_trade_loss = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(expert_zzlwa_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._slow_ma = None
        self._fast_ma = None
        self._pending_buy_signal = False
        self._pending_sell_signal = False
        self._original_buy_ready = False
        self._original_sell_ready = False
        self._zig_zag_direction = 0.0
        self._prev_slow = 0.0
        self._prev_fast = 0.0
        self._tracked_position = 0.0
        self._average_entry_price = 0.0
        self._last_closed_volume = 0.0
        self._last_trade_loss = False

    def OnStarted(self, time):
        super(expert_zzlwa_strategy, self).OnStarted(time)

        self.__highest = Highest()
        self.__highest.Length = GetZigZagDepth(self.zig_zag_term)
        self.__lowest = Lowest()
        self.__lowest.Length = GetZigZagDepth(self.zig_zag_term)
        self.__slow_ma = SmoothedMovingAverage()
        self.__slow_ma.Length = self.slow_ma_period
        self.__fast_ma = SimpleMovingAverage()
        self.__fast_ma.Length = self.fast_ma_period

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
        return expert_zzlwa_strategy()
