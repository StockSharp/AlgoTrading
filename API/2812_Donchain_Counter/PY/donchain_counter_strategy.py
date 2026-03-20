import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class donchain_counter_strategy(Strategy):
    def __init__(self):
        super(donchain_counter_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Donchian Period", "Lookback period for Donchian Channel", "Indicators")
        self._buffer_steps = self.Param("BufferSteps", 50) \
            .SetDisplay("Donchian Period", "Lookback period for Donchian Channel", "Indicators")
        self._trade_cooldown = self.Param("TradeCooldown", TimeSpan.FromMinutes(30) \
            .SetDisplay("Donchian Period", "Lookback period for Donchian Channel", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Donchian Period", "Lookback period for Donchian Channel", "Indicators")

        self._donchian = null!
        self._price_step = 0.0
        self._tolerance = 0.0
        self._current_upper = 0.0
        self._current_lower = 0.0
        self._previous_upper = 0.0
        self._previous_lower = 0.0
        self._earlier_upper = 0.0
        self._earlier_lower = 0.0
        self._long_stop_level = None
        self._short_stop_level = None
        self._last_trade_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchain_counter_strategy, self).OnReseted()
        self._donchian = null!
        self._price_step = 0.0
        self._tolerance = 0.0
        self._current_upper = 0.0
        self._current_lower = 0.0
        self._previous_upper = 0.0
        self._previous_lower = 0.0
        self._earlier_upper = 0.0
        self._earlier_lower = 0.0
        self._long_stop_level = None
        self._short_stop_level = None
        self._last_trade_time = None

    def OnStarted(self, time):
        super(donchain_counter_strategy, self).OnStarted(time)

        self.__donchian = DonchianChannels()
        self.__donchian.Length = self.channel_period

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
        return donchain_counter_strategy()
