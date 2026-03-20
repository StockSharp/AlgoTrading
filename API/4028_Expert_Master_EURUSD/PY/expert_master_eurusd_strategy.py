import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class expert_master_eurusd_strategy(Strategy):
    def __init__(self):
        super(expert_master_eurusd_strategy, self).__init__()

        self._trailing_points = self.Param("TrailingPoints", 25) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._fixed_volume = self.Param("FixedVolume", 1) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._risk_percent = self.Param("RiskPercent", 0.01) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._macd_fast_period = self.Param("MacdFastPeriod", 5) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 15) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 3) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._upper_macd_threshold = self.Param("UpperMacdThreshold", 10) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._lower_macd_threshold = self.Param("LowerMacdThreshold", -10) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._short_current_threshold = self.Param("ShortCurrentThreshold", -20) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")

        self._macd = null!
        self._macd_main0 = None
        self._macd_main1 = None
        self._macd_main2 = None
        self._macd_main3 = None
        self._signal0 = None
        self._signal1 = None
        self._signal2 = None
        self._signal3 = None
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_trailing_stop = None
        self._short_trailing_stop = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(expert_master_eurusd_strategy, self).OnReseted()
        self._macd = null!
        self._macd_main0 = None
        self._macd_main1 = None
        self._macd_main2 = None
        self._macd_main3 = None
        self._signal0 = None
        self._signal1 = None
        self._signal2 = None
        self._signal3 = None
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_trailing_stop = None
        self._short_trailing_stop = None

    def OnStarted(self, time):
        super(expert_master_eurusd_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return expert_master_eurusd_strategy()
