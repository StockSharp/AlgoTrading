import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class my_ts15_strategy(Strategy):
    def __init__(self):
        super(my_ts15_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(120) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 100) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._trail_multiplier = self.Param("TrailMultiplier", 3) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 12) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._entry_price = 0.0
        self._best_price = 0.0
        self._was_bullish = False
        self._has_prev_signal = False
        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(my_ts15_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._best_price = 0.0
        self._was_bullish = False
        self._has_prev_signal = False
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(my_ts15_strategy, self).OnStarted(time)

        self._wma = WeightedMovingAverage()
        self._wma.Length = self.ma_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._wma, self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return my_ts15_strategy()
