import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class master_exit_plan_strategy(Strategy):
    def __init__(self):
        super(master_exit_plan_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 20) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._slow_period = self.Param("SlowPeriod", 60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._atr_multiplier = self.Param("AtrMultiplier", 3) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._entry_price = 0.0
        self._trail_stop = 0.0
        self._was_bullish = False
        self._has_trend_state = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(master_exit_plan_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._trail_stop = 0.0
        self._was_bullish = False
        self._has_trend_state = False

    def OnStarted(self, time):
        super(master_exit_plan_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.fast_period
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.slow_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return master_exit_plan_strategy()
