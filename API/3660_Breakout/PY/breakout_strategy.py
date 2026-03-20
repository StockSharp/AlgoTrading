import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class breakout_strategy(Strategy):
    def __init__(self):
        super(breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Candle Type", "Primary timeframe used for calculations", "General")
        self._entry_period = self.Param("EntryPeriod", 20) \
            .SetDisplay("Candle Type", "Primary timeframe used for calculations", "General")
        self._entry_shift = self.Param("EntryShift", 1) \
            .SetDisplay("Candle Type", "Primary timeframe used for calculations", "General")
        self._exit_period = self.Param("ExitPeriod", 20) \
            .SetDisplay("Candle Type", "Primary timeframe used for calculations", "General")
        self._exit_shift = self.Param("ExitShift", 1) \
            .SetDisplay("Candle Type", "Primary timeframe used for calculations", "General")
        self._use_middle_line = self.Param("UseMiddleLine", True) \
            .SetDisplay("Candle Type", "Primary timeframe used for calculations", "General")
        self._risk_per_trade = self.Param("RiskPerTrade", 0.01) \
            .SetDisplay("Candle Type", "Primary timeframe used for calculations", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 4) \
            .SetDisplay("Candle Type", "Primary timeframe used for calculations", "General")

        self._entry_highest = None
        self._entry_lowest = None
        self._exit_highest = None
        self._exit_lowest = None
        self._entry_high_shift = None
        self._entry_low_shift = None
        self._exit_high_shift = None
        self._exit_low_shift = None
        self._cooldown_remaining = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(breakout_strategy, self).OnReseted()
        self._entry_highest = None
        self._entry_lowest = None
        self._exit_highest = None
        self._exit_lowest = None
        self._entry_high_shift = None
        self._entry_low_shift = None
        self._exit_high_shift = None
        self._exit_low_shift = None
        self._cooldown_remaining = 0.0

    def OnStarted(self, time):
        super(breakout_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


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
        return breakout_strategy()
