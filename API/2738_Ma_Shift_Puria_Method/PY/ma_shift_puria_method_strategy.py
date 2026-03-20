import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class ma_shift_puria_method_strategy(Strategy):
    def __init__(self):
        super(ma_shift_puria_method_strategy, self).__init__()

        self._use_manual_volume = self.Param("UseManualVolume", True) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._manual_volume = self.Param("ManualVolume", 0.1) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._risk_percent = self.Param("RiskPercent", 9) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 45) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 75) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 15) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._max_positions = self.Param("MaxPositions", 1) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._shift_min_pips = self.Param("ShiftMinPips", 20) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._fast_length = self.Param("FastLength", 14) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._slow_length = self.Param("SlowLength", 80) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._macd_fast = self.Param("MacdFast", 11) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._macd_slow = self.Param("MacdSlow", 102) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._use_fractal_trailing = self.Param("UseFractalTrailing", False) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Manual Volume", "Use fixed trade volume", "Risk")

        self._fast_ema = null!
        self._slow_ema = null!
        self._macd = null!
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._fast_prev3 = None
        self._slow_prev1 = None
        self._slow_prev2 = None
        self._slow_prev3 = None
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None
        self._fractal_count = 0.0
        self._last_upper_fractal = None
        self._last_lower_fractal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_shift_puria_method_strategy, self).OnReseted()
        self._fast_ema = null!
        self._slow_ema = null!
        self._macd = null!
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._fast_prev3 = None
        self._slow_prev1 = None
        self._slow_prev2 = None
        self._slow_prev3 = None
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None
        self._fractal_count = 0.0
        self._last_upper_fractal = None
        self._last_lower_fractal = None

    def OnStarted(self, time):
        super(ma_shift_puria_method_strategy, self).OnStarted(time)

        self.__fast_ema = ExponentialMovingAverage()
        self.__fast_ema.Length = self.fast_length
        self.__slow_ema = ExponentialMovingAverage()
        self.__slow_ema.Length = self.slow_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__fast_ema, self.__slow_ema, _macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ma_shift_puria_method_strategy()
