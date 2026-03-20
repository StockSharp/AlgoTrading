import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vortex_indicator_breakout_strategy(Strategy):
    def __init__(self):
        super(vortex_indicator_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._fast_ema_length = self.Param("FastEmaLength", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._slow_ema_length = self.Param("SlowEmaLength", 28) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vortex_indicator_breakout_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(vortex_indicator_breakout_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.fast_ema_length
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.slow_ema_length
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return vortex_indicator_breakout_strategy()
