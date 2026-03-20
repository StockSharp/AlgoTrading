import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class frank_ud_minimal_strategy(Strategy):
    def __init__(self):
        super(frank_ud_minimal_strategy, self).__init__()

        self._take_profit_pips = self.Param("TakeProfitPips", 65) \
            .SetDisplay("Profit trigger (pips)", "Pip profit that forces an exit of all positions.", "Risk")
        self._re_entry_pips = self.Param("ReEntryPips", 41) \
            .SetDisplay("Profit trigger (pips)", "Pip profit that forces an exit of all positions.", "Risk")
        self._initial_volume = self.Param("InitialVolume", 0.1) \
            .SetDisplay("Profit trigger (pips)", "Pip profit that forces an exit of all positions.", "Risk")
        self._minimum_free_margin_ratio = self.Param("MinimumFreeMarginRatio", 0.5) \
            .SetDisplay("Profit trigger (pips)", "Pip profit that forces an exit of all positions.", "Risk")
        self._extra_take_profit_pips = self.Param("ExtraTakeProfitPips", 25) \
            .SetDisplay("Profit trigger (pips)", "Pip profit that forces an exit of all positions.", "Risk")

        self._long_entries = new()
        self._short_entries = new()
        self._order_actions = new()
        self._point_value = 0.0
        self._take_profit_threshold = 0.0
        self._take_profit_distance = 0.0
        self._re_entry_distance = 0.0
        self._base_volume = 0.0
        self._last_bid = 0.0
        self._last_ask = 0.0

    def OnReseted(self):
        super(frank_ud_minimal_strategy, self).OnReseted()
        self._long_entries = new()
        self._short_entries = new()
        self._order_actions = new()
        self._point_value = 0.0
        self._take_profit_threshold = 0.0
        self._take_profit_distance = 0.0
        self._re_entry_distance = 0.0
        self._base_volume = 0.0
        self._last_bid = 0.0
        self._last_ask = 0.0

    def OnStarted(self, time):
        super(frank_ud_minimal_strategy, self).OnStarted(time)


    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return frank_ud_minimal_strategy()
