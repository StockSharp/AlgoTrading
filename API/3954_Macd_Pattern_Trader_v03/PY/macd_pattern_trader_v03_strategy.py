import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergence, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class macd_pattern_trader_v03_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_v03_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._fast_ema_length = self.Param("FastEmaLength", 5) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._slow_ema_length = self.Param("SlowEmaLength", 13) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._upper_threshold = self.Param("UpperThreshold", 50) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._upper_activation = self.Param("UpperActivation", 30) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._lower_threshold = self.Param("LowerThreshold", -50) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._lower_activation = self.Param("LowerActivation", -30) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._ema_one_length = self.Param("EmaOneLength", 7) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._ema_two_length = self.Param("EmaTwoLength", 21) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._sma_length = self.Param("SmaLength", 98) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._ema_four_length = self.Param("EmaFourLength", 365) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._profit_threshold = self.Param("ProfitThreshold", 5) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")

        self._previous_macd = None
        self._older_macd = None
        self._entry_price = 0.0
        self._is_above_upper_activation = False
        self._first_upper_drop_confirmed = False
        self._second_upper_drop_confirmed = False
        self._sell_ready = False
        self._first_upper_peak = 0.0
        self._second_upper_peak = 0.0
        self._is_below_lower_activation = False
        self._first_lower_rise_confirmed = False
        self._second_lower_rise_confirmed = False
        self._buy_ready = False
        self._first_lower_trough = 0.0
        self._second_lower_trough = 0.0
        self._ema_two_value = None
        self._sma_value = None
        self._ema_four_value = None
        self._previous_candle = None
        self._long_scale_stage = 0.0
        self._short_scale_stage = 0.0
        self._initial_long_position = 0.0
        self._initial_short_position = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_pattern_trader_v03_strategy, self).OnReseted()
        self._previous_macd = None
        self._older_macd = None
        self._entry_price = 0.0
        self._is_above_upper_activation = False
        self._first_upper_drop_confirmed = False
        self._second_upper_drop_confirmed = False
        self._sell_ready = False
        self._first_upper_peak = 0.0
        self._second_upper_peak = 0.0
        self._is_below_lower_activation = False
        self._first_lower_rise_confirmed = False
        self._second_lower_rise_confirmed = False
        self._buy_ready = False
        self._first_lower_trough = 0.0
        self._second_lower_trough = 0.0
        self._ema_two_value = None
        self._sma_value = None
        self._ema_four_value = None
        self._previous_candle = None
        self._long_scale_stage = 0.0
        self._short_scale_stage = 0.0
        self._initial_long_position = 0.0
        self._initial_short_position = 0.0

    def OnStarted(self, time):
        super(macd_pattern_trader_v03_strategy, self).OnStarted(time)

        self._ema_one = ExponentialMovingAverage()
        self._ema_one.Length = self.ema_one_length
        self._ema_two = ExponentialMovingAverage()
        self._ema_two.Length = self.ema_two_length
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_length
        self._ema_four = ExponentialMovingAverage()
        self._ema_four.Length = self.ema_four_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(macd, self._ema_one, self._ema_two, self._sma, self._ema_four, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return macd_pattern_trader_v03_strategy()
