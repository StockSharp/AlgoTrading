import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class executor_ao_strategy(Strategy):
    def __init__(self):
        super(executor_ao_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Trade Volume", "Fixed order size", "Risk")
        self._ao_short_period = self.Param("AoShortPeriod", 5) \
            .SetDisplay("Trade Volume", "Fixed order size", "Risk")
        self._ao_long_period = self.Param("AoLongPeriod", 34) \
            .SetDisplay("Trade Volume", "Fixed order size", "Risk")
        self._minimum_ao_indent = self.Param("MinimumAoIndent", 0.001) \
            .SetDisplay("Trade Volume", "Fixed order size", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Trade Volume", "Fixed order size", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Trade Volume", "Fixed order size", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5) \
            .SetDisplay("Trade Volume", "Fixed order size", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trade Volume", "Fixed order size", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Trade Volume", "Fixed order size", "Risk")

        self._ao = null!
        self._current_ao = None
        self._previous_ao = None
        self._previous_ao2 = None
        self._pip_size = 0.0
        self._long_entry_price = None
        self._long_stop = None
        self._long_take = None
        self._short_entry_price = None
        self._short_stop = None
        self._short_take = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(executor_ao_strategy, self).OnReseted()
        self._ao = null!
        self._current_ao = None
        self._previous_ao = None
        self._previous_ao2 = None
        self._pip_size = 0.0
        self._long_entry_price = None
        self._long_stop = None
        self._long_take = None
        self._short_entry_price = None
        self._short_stop = None
        self._short_take = None

    def OnStarted(self, time):
        super(executor_ao_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return executor_ao_strategy()
