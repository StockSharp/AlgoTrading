import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class grid_ea_pro_strategy(Strategy):
    def __init__(self):
        super(grid_ea_pro_strategy, self).__init__()

        self._mode = self.Param("Mode", GridTradeModes.Both) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._entry_mode = self.Param("EntryMode", GridEntryModes.Rsi) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._rsi_period = self.Param("RsiPeriod", 10) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._rsi_upper_level = self.Param("RsiUpperLevel", 70) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._rsi_lower_level = self.Param("RsiLowerLevel", 30) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._distance = self.Param("Distance", 50) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._timer_seconds = self.Param("TimerSeconds", 10) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._initial_volume = self.Param("InitialVolume", 0.01) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._from_balance = self.Param("FromBalance", 1000) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._risk_per_trade = self.Param("RiskPerTrade", 0) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._lot_multiplier = self.Param("LotMultiplier", 1.1) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._max_lot = self.Param("MaxLot", 999.9) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._step_orders = self.Param("StepOrders", 100) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._step_multiplier = self.Param("StepMultiplier", 1.1) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._max_step = self.Param("MaxStep", 1000) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._overlap_orders = self.Param("OverlapOrders", 5) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._overlap_pips = self.Param("OverlapPips", 10) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._stop_loss = self.Param("StopLoss", -1) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._take_profit = self.Param("TakeProfit", 500) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._break_even_stop = self.Param("BreakEvenStop", -1) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._break_even_step = self.Param("BreakEvenStep", 10) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._trailing_stop = self.Param("TrailingStop", 50) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._trailing_step = self.Param("TrailingStep", 50) \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._start_time = self.Param("StartTime", "00:00") \
            .SetDisplay("Mode", "Allowed trade direction", "General")
        self._end_time = self.Param("EndTime", "00:00") \
            .SetDisplay("Mode", "Allowed trade direction", "General")

        self._long_volumes = new()
        self._short_volumes = new()
        self._rsi = null!
        self._tick_size = 0.0
        self._step_value = 0.0
        self._tick_value = 0.0
        self._last_long_price = 0.0
        self._last_short_price = 0.0
        self._last_long_volume = 0.0
        self._last_short_volume = 0.0
        self._long_stop = None
        self._short_stop = None
        self._long_take = None
        self._short_take = None
        self._long_break_even = False
        self._short_break_even = False
        self._long_trail_anchor = 0.0
        self._short_trail_anchor = 0.0
        self._long_next_level = None
        self._short_next_level = None
        self._pending_long = None
        self._pending_short = None
        self._next_timer = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grid_ea_pro_strategy, self).OnReseted()
        self._long_volumes = new()
        self._short_volumes = new()
        self._rsi = null!
        self._tick_size = 0.0
        self._step_value = 0.0
        self._tick_value = 0.0
        self._last_long_price = 0.0
        self._last_short_price = 0.0
        self._last_long_volume = 0.0
        self._last_short_volume = 0.0
        self._long_stop = None
        self._short_stop = None
        self._long_take = None
        self._short_take = None
        self._long_break_even = False
        self._short_break_even = False
        self._long_trail_anchor = 0.0
        self._short_trail_anchor = 0.0
        self._long_next_level = None
        self._short_next_level = None
        self._pending_long = None
        self._pending_short = None
        self._next_timer = None

    def OnStarted(self, time):
        super(grid_ea_pro_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return grid_ea_pro_strategy()
