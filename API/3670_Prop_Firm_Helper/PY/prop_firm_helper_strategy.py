import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class prop_firm_helper_strategy(Strategy):
    def __init__(self):
        super(prop_firm_helper_strategy, self).__init__()

        self._entry_period = self.Param("EntryPeriod", 20) \
            .SetDisplay("Entry Period", "Number of candles used for breakout Donchian channel", "Entries")
        self._exit_period = self.Param("ExitPeriod", 10) \
            .SetDisplay("Entry Period", "Number of candles used for breakout Donchian channel", "Entries")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Entry Period", "Number of candles used for breakout Donchian channel", "Entries")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 4) \
            .SetDisplay("Entry Period", "Number of candles used for breakout Donchian channel", "Entries")

        self._entry_channel = None
        self._exit_channel = None
        self._entry_upper = 0.0
        self._entry_lower = 0.0
        self._exit_lower = 0.0
        self._exit_upper = 0.0
        self._prev_entry_upper = 0.0
        self._prev_entry_lower = 0.0
        self._has_values = False
        self._entry_price = 0.0
        self._cooldown_remaining = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(prop_firm_helper_strategy, self).OnReseted()
        self._entry_channel = None
        self._exit_channel = None
        self._entry_upper = 0.0
        self._entry_lower = 0.0
        self._exit_lower = 0.0
        self._exit_upper = 0.0
        self._prev_entry_upper = 0.0
        self._prev_entry_lower = 0.0
        self._has_values = False
        self._entry_price = 0.0
        self._cooldown_remaining = 0.0

    def OnStarted(self, time):
        super(prop_firm_helper_strategy, self).OnStarted(time)

        self.__entry_channel = DonchianChannels()
        self.__entry_channel.Length = self.entry_period
        self.__exit_channel = DonchianChannels()
        self.__exit_channel.Length = self.exit_period

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
        return prop_firm_helper_strategy()
