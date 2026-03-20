import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest, MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class js_sistem2_strategy(Strategy):
    def __init__(self):
        super(js_sistem2_strategy, self).__init__()

        self._min_balance = self.Param("MinBalance", 100) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 200) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 300) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._min_difference_pips = self.Param("MinDifferencePips", 5000) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._volatility_period = self.Param("VolatilityPeriod", 15) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._trailing_enabled = self.Param("TrailingEnabled", True) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._trailing_indent_pips = self.Param("TrailingIndentPips", 1) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._ma_fast_period = self.Param("MaFastPeriod", 12) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._ma_medium_period = self.Param("MaMediumPeriod", 26) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._ma_slow_period = self.Param("MaSlowPeriod", 50) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._osma_fast_period = self.Param("OsmaFastPeriod", 12) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._osma_slow_period = self.Param("OsmaSlowPeriod", 26) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._osma_signal_period = self.Param("OsmaSignalPeriod", 9) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._rvi_period = self.Param("RviPeriod", 10) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._rvi_signal_length = self.Param("RviSignalLength", 4) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._rvi_max = self.Param("RviMax", 0.02) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._rvi_min = self.Param("RviMin", -0.02) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")

        self._ema_fast = null!
        self._ema_medium = null!
        self._ema_slow = null!
        self._macd = null!
        self._highest = null!
        self._lowest = null!
        self._rvi = null!
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(js_sistem2_strategy, self).OnReseted()
        self._ema_fast = null!
        self._ema_medium = null!
        self._ema_slow = null!
        self._macd = null!
        self._highest = null!
        self._lowest = null!
        self._rvi = null!
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(js_sistem2_strategy, self).OnStarted(time)

        self.__ema_fast = ExponentialMovingAverage()
        self.__ema_fast.Length = self.ma_fast_period
        self.__ema_medium = ExponentialMovingAverage()
        self.__ema_medium.Length = self.ma_medium_period
        self.__ema_slow = ExponentialMovingAverage()
        self.__ema_slow.Length = self.ma_slow_period
        self.__highest = Highest()
        self.__highest.Length = self.volatility_period
        self.__lowest = Lowest()
        self.__lowest.Length = self.volatility_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__ema_fast, self.__ema_medium, self.__ema_slow, _macd, self.__highest, self.__lowest, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return js_sistem2_strategy()
