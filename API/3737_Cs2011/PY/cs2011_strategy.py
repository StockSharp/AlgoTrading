import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class cs2011_strategy(Strategy):
    def __init__(self):
        super(cs2011_strategy, self).__init__()

        self._target_volume = self.Param("TargetVolume", 1) \
            .SetDisplay("Target Volume", "Absolute position size targeted on entries", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2200) \
            .SetDisplay("Target Volume", "Absolute position size targeted on entries", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 0) \
            .SetDisplay("Target Volume", "Absolute position size targeted on entries", "Risk")
        self._fast_ema_period = self.Param("FastEmaPeriod", 30) \
            .SetDisplay("Target Volume", "Absolute position size targeted on entries", "Risk")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 50) \
            .SetDisplay("Target Volume", "Absolute position size targeted on entries", "Risk")
        self._signal_period = self.Param("SignalPeriod", 36) \
            .SetDisplay("Target Volume", "Absolute position size targeted on entries", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Target Volume", "Absolute position size targeted on entries", "Risk")

        self._macd_prev1 = None
        self._macd_prev2 = None
        self._signal_prev1 = None
        self._signal_prev2 = None
        self._signal_prev3 = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cs2011_strategy, self).OnReseted()
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._signal_prev1 = None
        self._signal_prev2 = None
        self._signal_prev3 = None

    def OnStarted(self, time):
        super(cs2011_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return cs2011_strategy()
