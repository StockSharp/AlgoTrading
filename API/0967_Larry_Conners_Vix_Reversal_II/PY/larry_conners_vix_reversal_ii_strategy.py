import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class larry_conners_vix_reversal_ii_strategy(Strategy):
    def __init__(self):
        super(larry_conners_vix_reversal_ii_strategy, self).__init__()

        self._vix = self.Param("Vix", None) \
            .SetDisplay("VIX Security", "VIX symbol", "Universe")
        self._rsi_period = self.Param("RsiPeriod", 25) \
            .SetDisplay("RSI Period", "RSI length", "Parameters")
        self._overbought = self.Param("OverboughtLevel", 61) \
            .SetDisplay("Overbought Level", "RSI overbought level", "Parameters")
        self._oversold = self.Param("OversoldLevel", 42) \
            .SetDisplay("Oversold Level", "RSI oversold level", "Parameters")
        self._min_holding = self.Param("MinHoldingDays", 7) \
            .SetDisplay("Min Holding Days", "Minimum holding period", "Risk Management")
        self._max_holding = self.Param("MaxHoldingDays", 12) \
            .SetDisplay("Max Holding Days", "Maximum holding period", "Risk Management")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk Management")
        self._cooldown_bars = self.Param("CooldownBars", 240) \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "Data")

        self._rsi = None
        self._prev_rsi = 0.0
        self._holding_days = 0
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def vix(self):
        return self._vix.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def overbought_level(self):
        return self._overbought.Value

    @property
    def oversold_level(self):
        return self._oversold.Value

    @property
    def min_holding_days(self):
        return self._min_holding.Value

    @property
    def max_holding_days(self):
        return self._max_holding.Value

    @property
    def max_entries(self):
        return self._max_entries.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(larry_conners_vix_reversal_ii_strategy, self).OnReseted()
        self._rsi = None
        self._prev_rsi = 0.0
        self._holding_days = 0
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(larry_conners_vix_reversal_ii_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._prev_rsi = 0.0
        self._holding_days = 0
        self._entries_executed = 0
        self._bars_since_signal = int(self.cooldown_bars)

        self.SubscribeCandles(self.candle_type, True, self.Security).Start()
        self.SubscribeCandles(self.candle_type, True, self.vix) \
            .Bind(self._rsi, self._process_vix).Start()

    def _process_vix(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        self._bars_since_signal += 1

        if not self._rsi.IsFormed:
            self._prev_rsi = rsi_val
            return

        ob = float(self.overbought_level)
        os_level = float(self.oversold_level)

        if self._holding_days > 0:
            self._holding_days += 1
            if self._holding_days >= int(self.min_holding_days) and self._holding_days >= int(self.max_holding_days):
                if self.Position > 0:
                    self.SellMarket(abs(self.Position))
                elif self.Position < 0:
                    self.BuyMarket(abs(self.Position))
                self._holding_days = 0
                self._bars_since_signal = 0

        cross_over = self._prev_rsi < ob and rsi_val >= ob
        cross_under = self._prev_rsi > os_level and rsi_val <= os_level

        if self._holding_days == 0 and self._entries_executed < int(self.max_entries) and self._bars_since_signal >= int(self.cooldown_bars):
            if cross_over and self.Position <= 0:
                self.BuyMarket()
                self._holding_days = 1
                self._entries_executed += 1
                self._bars_since_signal = 0
            elif cross_under and self.Position >= 0:
                self.SellMarket()
                self._holding_days = 1
                self._entries_executed += 1
                self._bars_since_signal = 0

        self._prev_rsi = rsi_val

    def CreateClone(self):
        return larry_conners_vix_reversal_ii_strategy()
