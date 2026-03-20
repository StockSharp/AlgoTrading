import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class osf_countertrend_strategy(Strategy):
    def __init__(self):
        super(osf_countertrend_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI length used in oscillator", "General")
        self._volume_per_point = self.Param("VolumePerPoint", 0.01) \
            .SetDisplay("RSI Period", "RSI length used in oscillator", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 150) \
            .SetDisplay("RSI Period", "RSI length used in oscillator", "General")
        self._cooldown_bars = self.Param("CooldownBars", 5) \
            .SetDisplay("RSI Period", "RSI length used in oscillator", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("RSI Period", "RSI length used in oscillator", "General")

        self._rsi = None
        self._cooldown = 0.0
        self._long_target = 0.0
        self._short_target = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(osf_countertrend_strategy, self).OnReseted()
        self._rsi = None
        self._cooldown = 0.0
        self._long_target = 0.0
        self._short_target = 0.0

    def OnStarted(self, time):
        super(osf_countertrend_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return osf_countertrend_strategy()
