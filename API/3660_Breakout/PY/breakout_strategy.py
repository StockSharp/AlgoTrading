import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class breakout_strategy(Strategy):
    def __init__(self):
        super(breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2)))
        self._entry_period = self.Param("EntryPeriod", 20)
        self._exit_period = self.Param("ExitPeriod", 20)
        self._use_middle_line = self.Param("UseMiddleLine", True)
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 4)

        self._entry_highs = []
        self._entry_lows = []
        self._exit_highs = []
        self._exit_lows = []
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
    def UseMiddleLine(self):
        return self._use_middle_line.Value

    @UseMiddleLine.setter
    def UseMiddleLine(self, value):
        self._use_middle_line.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    def OnReseted(self):
        super(breakout_strategy, self).OnReseted()
        self._entry_highs = []
        self._entry_lows = []
        self._exit_highs = []
        self._exit_lows = []
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(breakout_strategy, self).OnStarted2(time)
        self._entry_highs = []
        self._entry_lows = []
        self._exit_highs = []
        self._exit_lows = []
        self._cooldown_remaining = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        entry_period = self.EntryPeriod
        exit_period = self.ExitPeriod

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        # Update entry channel
        self._entry_highs.append(high)
        self._entry_lows.append(low)
        while len(self._entry_highs) > entry_period:
            self._entry_highs.pop(0)
            self._entry_lows.pop(0)

        # Update exit channel
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

        exit_middle = (exit_upper + exit_lower) / 2.0
        use_mid = self.UseMiddleLine
        exit_long = max(exit_middle, exit_lower) if use_mid else exit_lower
        exit_short = min(exit_middle, exit_upper) if use_mid else exit_upper

        trigger_long = entry_upper
        trigger_short = entry_lower
        cooldown_bars = self.SignalCooldownBars

        # Manage trailing exits before evaluating new entries
        if self.Position > 0 and low <= exit_long:
            self.SellMarket()
            self._cooldown_remaining = cooldown_bars
        elif self.Position < 0 and high >= exit_short:
            self.BuyMarket()
            self._cooldown_remaining = cooldown_bars

        # Enter long on breakout above channel
        if self._cooldown_remaining == 0 and self.Position <= 0 and high >= trigger_long:
            stop_distance = trigger_long - exit_long
            if stop_distance > 0:
                self.BuyMarket()
                self._cooldown_remaining = cooldown_bars
        # Enter short on breakout below channel
        elif self._cooldown_remaining == 0 and self.Position >= 0 and low <= trigger_short:
            stop_distance = exit_short - trigger_short
            if stop_distance > 0:
                self.SellMarket()
                self._cooldown_remaining = cooldown_bars

    def CreateClone(self):
        return breakout_strategy()
