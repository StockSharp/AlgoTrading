import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, ExponentialMovingAverage as EMA, JurikMovingAverage, SimpleMovingAverage as SMA, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class exp_x_bulls_bears_eyes_vol_direct_strategy(Strategy):
    def __init__(self):
        super(exp_x_bulls_bears_eyes_vol_direct_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._period = self.Param("Period", 13) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._gamma = self.Param("Gamma", 0.6) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._volume_source = self.Param("VolumeMode", VolumeSources.Tick) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._smoothing_method = self.Param("Method", SmoothingMethods.Sma) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._smoothing_length = self.Param("SmoothingLength", 12) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._smoothing_phase = self.Param("SmoothingPhase", 15) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._allow_buy_open = self.Param("AllowBuyOpen", True) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._allow_sell_open = self.Param("AllowSellOpen", True) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._allow_buy_close = self.Param("AllowBuyClose", True) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._allow_sell_close = self.Param("AllowSellClose", True) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._order_volume = self.Param("OrderVolume", 1) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Candle Type", "Timeframe used by the indicator", "General")

        self._ema = None
        self._histogram_smoother = None
        self._volume_smoother = None
        self._direction_history = new()
        self._l0 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._previous_smoothed_value = None
        self._previous_direction = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_x_bulls_bears_eyes_vol_direct_strategy, self).OnReseted()
        self._ema = None
        self._histogram_smoother = None
        self._volume_smoother = None
        self._direction_history = new()
        self._l0 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._previous_smoothed_value = None
        self._previous_direction = 0.0

    def OnStarted(self, time):
        super(exp_x_bulls_bears_eyes_vol_direct_strategy, self).OnStarted(time)

        self.__ema = EMA()
        self.__ema.Length = Math.Max(1

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return exp_x_bulls_bears_eyes_vol_direct_strategy()
