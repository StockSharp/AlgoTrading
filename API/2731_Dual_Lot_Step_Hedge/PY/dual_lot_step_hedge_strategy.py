import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class dual_lot_step_hedge_strategy(Strategy):
    def __init__(self):
        super(dual_lot_step_hedge_strategy, self).__init__()

        self._lot_multiplier = self.Param("LotMultiplier", 10) \
            .SetDisplay("Lot Multiplier", "Maximum lot multiplier over the minimal step", "Trading")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Lot Multiplier", "Maximum lot multiplier over the minimal step", "Trading")
        self._take_profit_pips = self.Param("TakeProfitPips", 150) \
            .SetDisplay("Lot Multiplier", "Maximum lot multiplier over the minimal step", "Trading")
        self._min_profit = self.Param("MinProfit", 27) \
            .SetDisplay("Lot Multiplier", "Maximum lot multiplier over the minimal step", "Trading")
        self._scaling_mode = self.Param("ScalingMode", LotScalingModes.HighToLow) \
            .SetDisplay("Lot Multiplier", "Maximum lot multiplier over the minimal step", "Trading")

        self._volume_step = 0.0
        self._max_volume = 0.0
        self._current_volume = 0.0
        self._pip_value = 0.0
        self._initial_equity = 0.0
        self._long_volume = 0.0
        self._short_volume = 0.0
        self._long_average_price = 0.0
        self._short_average_price = 0.0
        self._long_entry_in_progress = False
        self._short_entry_in_progress = False
        self._long_exit_in_progress = False
        self._short_exit_in_progress = False
        self._pending_long_entry_volume = 0.0
        self._pending_short_entry_volume = 0.0
        self._pending_long_exit_volume = 0.0
        self._pending_short_exit_volume = 0.0
        self._reset_requested = False

    def OnReseted(self):
        super(dual_lot_step_hedge_strategy, self).OnReseted()
        self._volume_step = 0.0
        self._max_volume = 0.0
        self._current_volume = 0.0
        self._pip_value = 0.0
        self._initial_equity = 0.0
        self._long_volume = 0.0
        self._short_volume = 0.0
        self._long_average_price = 0.0
        self._short_average_price = 0.0
        self._long_entry_in_progress = False
        self._short_entry_in_progress = False
        self._long_exit_in_progress = False
        self._short_exit_in_progress = False
        self._pending_long_entry_volume = 0.0
        self._pending_short_entry_volume = 0.0
        self._pending_long_exit_volume = 0.0
        self._pending_short_exit_volume = 0.0
        self._reset_requested = False

    def OnStarted(self, time):
        super(dual_lot_step_hedge_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(TimeSpan.FromMinutes(5)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return dual_lot_step_hedge_strategy()
