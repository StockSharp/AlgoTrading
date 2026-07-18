import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class prop_firm_helper_strategy(Strategy):
    def __init__(self):
        super(prop_firm_helper_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._entry_period = self.Param("EntryPeriod", 20)
        self._exit_period = self.Param("ExitPeriod", 10)
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 4)

        self._entry_highs = []
        self._entry_lows = []
        self._exit_highs = []
        self._exit_lows = []
        self._prev_entry_upper = 0.0
        self._prev_entry_lower = 0.0
        self._has_values = False
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EntryPeriod(self):
        return self._entry_period.Value

    @EntryPeriod.setter
    def EntryPeriod(self, value):
        self._entry_period.Value = value

    @property
    def ExitPeriod(self):
        return self._exit_period.Value

    @ExitPeriod.setter
    def ExitPeriod(self, value):
        self._exit_period.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    def OnReseted(self):
        super(prop_firm_helper_strategy, self).OnReseted()
        self._entry_highs = []
        self._entry_lows = []
        self._exit_highs = []
        self._exit_lows = []
        self._prev_entry_upper = 0.0
        self._prev_entry_lower = 0.0
        self._has_values = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(prop_firm_helper_strategy, self).OnStarted2(time)
        self._entry_highs = []
        self._entry_lows = []
        self._exit_highs = []
        self._exit_lows = []
        self._prev_entry_upper = 0.0
        self._prev_entry_lower = 0.0
        self._has_values = False
        self._cooldown_remaining = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        entry_period = self.EntryPeriod
        exit_period = self.ExitPeriod

        self._entry_highs.append(high)
        self._entry_lows.append(low)
        while len(self._entry_highs) > entry_period:
            self._entry_highs.pop(0)
            self._entry_lows.pop(0)

        self._exit_highs.append(high)
        self._exit_lows.append(low)
        while len(self._exit_highs) > exit_period:
            self._exit_highs.pop(0)
            self._exit_lows.pop(0)

        if len(self._entry_highs) < entry_period or len(self._exit_highs) < exit_period:
            return

        entry_upper = max(self._entry_highs)
        entry_lower = min(self._entry_lows)
        exit_upper = max(self._exit_highs)
        exit_lower = min(self._exit_lows)

        if not self._has_values:
            self._prev_entry_upper = entry_upper
            self._prev_entry_lower = entry_lower
            self._has_values = True
            return

        cooldown = self.SignalCooldownBars

        # Exit logic
        if self.Position > 0 and close < exit_lower:
            self.SellMarket()
            self._cooldown_remaining = cooldown
            self._prev_entry_upper = entry_upper
            self._prev_entry_lower = entry_lower
            return
        elif self.Position < 0 and close > exit_upper:
            self.BuyMarket()
            self._cooldown_remaining = cooldown
            self._prev_entry_upper = entry_upper
            self._prev_entry_lower = entry_lower
            return

        # Entry logic
        if self._cooldown_remaining == 0 and self._prev_entry_upper > 0 and close > self._prev_entry_upper and self.Position <= 0:
            self.BuyMarket()
            self._cooldown_remaining = cooldown
        elif self._cooldown_remaining == 0 and self._prev_entry_lower > 0 and close < self._prev_entry_lower and self.Position >= 0:
            self.SellMarket()
            self._cooldown_remaining = cooldown

        self._prev_entry_upper = entry_upper
        self._prev_entry_lower = entry_lower

    def CreateClone(self):
        return prop_firm_helper_strategy()
