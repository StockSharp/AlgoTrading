import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ka_gold_bot_strategy(Strategy):
    def __init__(self):
        super(ka_gold_bot_strategy, self).__init__()

        self._keltner_period = self.Param("KeltnerPeriod", 50) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._fast_ema_period = self.Param("FastEmaPeriod", 10) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 200) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._base_volume = self.Param("BaseVolume", 0.01) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._use_risk_percent = self.Param("UseRiskPercent", True) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._risk_percent = self.Param("RiskPercent", 1) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._stop_loss_pips = self.Param("StopLossPips", 500) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._take_profit_pips = self.Param("TakeProfitPips", 500) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._trailing_trigger_pips = self.Param("TrailingTriggerPips", 300) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 300) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._trailing_step_pips = self.Param("TrailingStepPips", 100) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._use_time_filter = self.Param("UseTimeFilter", True) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._start_hour = self.Param("StartHour", 2) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._start_minute = self.Param("StartMinute", 30) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._end_hour = self.Param("EndHour", 21) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._end_minute = self.Param("EndMinute", 0) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._max_spread_points = self.Param("MaxSpreadPoints", 65) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators")

        self._fast_ema = null!
        self._slow_ema = null!
        self._keltner_ema = null!
        self._range_average = null!
        self._close_prev1 = None
        self._close_prev2 = None
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._slow_prev1 = None
        self._upper_prev1 = None
        self._upper_prev2 = None
        self._lower_prev1 = None
        self._lower_prev2 = None
        self._pip_size = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_trigger_distance = 0.0
        self._trailing_stop_distance = 0.0
        self._trailing_step_distance = 0.0
        self._long_trailing_armed = False
        self._short_trailing_armed = False
        self._stop_order = None
        self._take_profit_order = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ka_gold_bot_strategy, self).OnReseted()
        self._fast_ema = null!
        self._slow_ema = null!
        self._keltner_ema = null!
        self._range_average = null!
        self._close_prev1 = None
        self._close_prev2 = None
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._slow_prev1 = None
        self._upper_prev1 = None
        self._upper_prev2 = None
        self._lower_prev1 = None
        self._lower_prev2 = None
        self._pip_size = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_trigger_distance = 0.0
        self._trailing_stop_distance = 0.0
        self._trailing_step_distance = 0.0
        self._long_trailing_armed = False
        self._short_trailing_armed = False
        self._stop_order = None
        self._take_profit_order = None

    def OnStarted(self, time):
        super(ka_gold_bot_strategy, self).OnStarted(time)

        self.__fast_ema = ExponentialMovingAverage()
        self.__fast_ema.Length = self.fast_ema_period
        self.__slow_ema = ExponentialMovingAverage()
        self.__slow_ema.Length = self.slow_ema_period
        self.__keltner_ema = ExponentialMovingAverage()
        self.__keltner_ema.Length = self.keltner_period
        self.__range_average = SimpleMovingAverage()
        self.__range_average.Length = self.keltner_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__fast_ema, self.__slow_ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ka_gold_bot_strategy()
