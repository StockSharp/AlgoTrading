import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class equal_volume_range_bars_strategy(Strategy):
    def __init__(self):
        super(equal_volume_range_bars_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(equal_volume_range_bars_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(equal_volume_range_bars_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return equal_volume_range_bars_strategy()
