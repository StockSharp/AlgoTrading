import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class peter_panel_strategy(Strategy):
    def __init__(self):
        super(peter_panel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(peter_panel_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(peter_panel_strategy, self).OnStarted(time)


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
        return peter_panel_strategy()
