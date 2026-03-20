import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage as EMA, SimpleMovingAverage as SMA, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class starter_strategy(Strategy):
    def __init__(self):
        super(starter_strategy, self).__init__()

        self._maximum_risk = self.Param("MaximumRisk", 0.02) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._decrease_factor = self.Param("DecreaseFactor", 3) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._cci_level = self.Param("CciLevel", 100) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._cci_current_bar = self.Param("CciCurrentBar", 0) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._cci_previous_bar = self.Param("CciPreviousBar", 1) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._ma_period = self.Param("MaPeriod", 120) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._ma_method = self.Param("MaMethod", MovingAverageMethods.Simple) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._ma_current_bar = self.Param("MaCurrentBar", 0) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._ma_delta = self.Param("MaDelta", 0.001) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._stop_loss_pips = self.Param("StopLossPips", 0) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")

        self._cci = null!
        self._moving_average = null!
        self._cci_history = new()
        self._ma_history = new()
        self._pip_size = 0.0
        self._history_capacity = 0.0
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop = None
        self._short_stop = None
        self._signed_position = 0.0
        self._last_entry_side = None
        self._last_entry_price = 0.0
        self._consecutive_losses = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(starter_strategy, self).OnReseted()
        self._cci = null!
        self._moving_average = null!
        self._cci_history = new()
        self._ma_history = new()
        self._pip_size = 0.0
        self._history_capacity = 0.0
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop = None
        self._short_stop = None
        self._signed_position = 0.0
        self._last_entry_side = None
        self._last_entry_price = 0.0
        self._consecutive_losses = 0.0

    def OnStarted(self, time):
        super(starter_strategy, self).OnStarted(time)

        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__cci, _movingAverage, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return starter_strategy()
