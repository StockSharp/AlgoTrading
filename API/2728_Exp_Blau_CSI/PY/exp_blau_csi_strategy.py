import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, JurikMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_blau_csi_strategy(Strategy):
    def __init__(self):
        super(exp_blau_csi_strategy, self).__init__()

        self._entry_mode = self.Param("EntryMode", BlauCsiEntryModes.Breakdown) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._smooth_method = self.Param("SmoothingMethod", BlauCsiSmoothMethods.Exponential) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._momentum_length = self.Param("MomentumLength", 1) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._first_smooth_length = self.Param("FirstSmoothingLength", 10) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._second_smooth_length = self.Param("SecondSmoothingLength", 3) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._third_smooth_length = self.Param("ThirdSmoothingLength", 2) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._smoothing_phase = self.Param("SmoothingPhase", 15) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._first_price = self.Param("FirstPrice", BlauCsiAppliedPrices.Close) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._second_price = self.Param("SecondPrice", BlauCsiAppliedPrices.Open) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._allow_long_entries = self.Param("AllowLongEntries", True) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._allow_short_entries = self.Param("AllowShortEntries", True) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._allow_long_exits = self.Param("AllowLongExits", True) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._allow_short_exits = self.Param("AllowShortExits", True) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._start_date = self.Param("StartDate", new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")
        self._end_date = self.Param("EndDate", new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero) \
            .SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters")

        self._blau_csi = null!
        self._indicator_values = new()
        self._stop_price = None
        self._take_price = None
        self._window = new()
        self._momentum_stage1 = None
        self._momentum_stage2 = None
        self._momentum_stage3 = None
        self._range_stage1 = None
        self._range_stage2 = None
        self._range_stage3 = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_blau_csi_strategy, self).OnReseted()
        self._blau_csi = null!
        self._indicator_values = new()
        self._stop_price = None
        self._take_price = None
        self._window = new()
        self._momentum_stage1 = None
        self._momentum_stage2 = None
        self._momentum_stage3 = None
        self._range_stage1 = None
        self._range_stage2 = None
        self._range_stage3 = None

    def OnStarted(self, time):
        super(exp_blau_csi_strategy, self).OnStarted(time)

        self.__blau_csi = BlauCsiIndicator()
        self.__blau_csi.SmoothMethod = self.smoothing_method
        self.__blau_csi.MomentumLength = self.momentum_length
        self.__blau_csi.FirstSmoothingLength = self.first_smoothing_length
        self.__blau_csi.SecondSmoothingLength = self.second_smoothing_length
        self.__blau_csi.ThirdSmoothingLength = self.third_smoothing_length
        self.__blau_csi.Phase = self.smoothing_phase
        self.__blau_csi.FirstPrice = self.first_price
        self.__blau_csi.SecondPrice = self.second_price

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
        return exp_blau_csi_strategy()
