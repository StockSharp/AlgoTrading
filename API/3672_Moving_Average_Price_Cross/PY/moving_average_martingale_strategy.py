import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage as SMA
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class moving_average_martingale_strategy(Strategy):
    def __init__(self):
        super(moving_average_martingale_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 50) \
            .SetDisplay("MA period", "Length of the simple moving average used for entries.", "Indicator")
        self._starting_volume = self.Param("StartingVolume", 1) \
            .SetDisplay("MA period", "Length of the simple moving average used for entries.", "Indicator")
        self._max_volume = self.Param("MaxVolume", 5) \
            .SetDisplay("MA period", "Length of the simple moving average used for entries.", "Indicator")
        self._take_profit_points = self.Param("TakeProfitPoints", 100) \
            .SetDisplay("MA period", "Length of the simple moving average used for entries.", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 300) \
            .SetDisplay("MA period", "Length of the simple moving average used for entries.", "Indicator")
        self._volume_multiplier = self.Param("VolumeMultiplier", 2) \
            .SetDisplay("MA period", "Length of the simple moving average used for entries.", "Indicator")
        self._target_multiplier = self.Param("TargetMultiplier", 2) \
            .SetDisplay("MA period", "Length of the simple moving average used for entries.", "Indicator")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("MA period", "Length of the simple moving average used for entries.", "Indicator")

        self._sma = None
        self._previous_close = None
        self._previous_ma = None
        self._current_close = None
        self._current_ma = None
        self._pip_size = 0.0
        self._current_volume = 0.0
        self._current_take_profit_points = 0.0
        self._current_stop_loss_points = 0.0
        self._last_realized_pn_l = 0.0
        self._previous_position = 0.0
        self._last_trade_result = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(moving_average_martingale_strategy, self).OnReseted()
        self._sma = None
        self._previous_close = None
        self._previous_ma = None
        self._current_close = None
        self._current_ma = None
        self._pip_size = 0.0
        self._current_volume = 0.0
        self._current_take_profit_points = 0.0
        self._current_stop_loss_points = 0.0
        self._last_realized_pn_l = 0.0
        self._previous_position = 0.0
        self._last_trade_result = 0.0

    def OnStarted(self, time):
        super(moving_average_martingale_strategy, self).OnStarted(time)

        self.__sma = SMA()
        self.__sma.Length = self.ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return moving_average_martingale_strategy()
