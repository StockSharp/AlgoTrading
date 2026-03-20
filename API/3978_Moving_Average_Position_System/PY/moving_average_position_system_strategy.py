import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA, SimpleMovingAverage as SMA, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class moving_average_position_system_strategy(Strategy):
    def __init__(self):
        super(moving_average_position_system_strategy, self).__init__()

        self._ma_type = self.Param("MaType", MovingAverageModes.LinearWeighted) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")
        self._ma_shift = self.Param("MaShift", 0) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")
        self._initial_volume = self.Param("InitialVolume", 0.1) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")
        self._start_volume = self.Param("StartVolume", 0.1) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")
        self._max_volume = self.Param("MaxVolume", 10) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")
        self._loss_threshold_pips = self.Param("LossThresholdPips", 90) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")
        self._profit_threshold_pips = self.Param("ProfitThresholdPips", 170) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")
        self._take_profit_pips = self.Param("TakeProfitPips", 1000) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")
        self._use_money_management = self.Param("UseMoneyManagement", True) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("MA Type", "Moving average method", "Indicators")

        self._close_history = new()
        self._ma_history = new()
        self._current_volume = 0.0
        self._cycle_start_realized_pn_l = 0.0
        self._price_step = 0.0
        self._step_price = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(moving_average_position_system_strategy, self).OnReseted()
        self._close_history = new()
        self._ma_history = new()
        self._current_volume = 0.0
        self._cycle_start_realized_pn_l = 0.0
        self._price_step = 0.0
        self._step_price = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(moving_average_position_system_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(movingAverage, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return moving_average_position_system_strategy()
