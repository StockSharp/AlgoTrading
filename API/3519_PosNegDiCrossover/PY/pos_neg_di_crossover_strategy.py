import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class pos_neg_di_crossover_strategy(Strategy):
    def __init__(self):
        super(pos_neg_di_crossover_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")
        self._use_time_filter = self.Param("UseTimeFilter", True) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")
        self._start_time = self.Param("StartTime", new TimeSpan(0, 0, 0) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")
        self._stop_time = self.Param("StopTime", new TimeSpan(23, 59, 0) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")
        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 10) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 10) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")
        self._martingale_multiplier = self.Param("MartingaleMultiplier", 2) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")
        self._martingale_cycle_limit = self.Param("MartingaleCycleLimit", 5) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")

        self._previous_plus_di = 0.0
        self._previous_minus_di = 0.0
        self._di_initialized = False
        self._cycle_active = False
        self._cycle_side = None
        self._current_volume = 0.0
        self._current_cycle = 0.0
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._awaiting_cycle_resolution = False
        self._last_exit_was_loss = False
        self._last_signal_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(pos_neg_di_crossover_strategy, self).OnReseted()
        self._previous_plus_di = 0.0
        self._previous_minus_di = 0.0
        self._di_initialized = False
        self._cycle_active = False
        self._cycle_side = None
        self._current_volume = 0.0
        self._current_cycle = 0.0
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._awaiting_cycle_resolution = False
        self._last_exit_was_loss = False
        self._last_signal_time = None

    def OnStarted(self, time):
        super(pos_neg_di_crossover_strategy, self).OnStarted(time)

        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.adx_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._adx, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return pos_neg_di_crossover_strategy()
