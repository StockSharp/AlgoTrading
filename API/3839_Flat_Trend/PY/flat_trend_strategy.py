import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class flat_trend_strategy(Strategy):
    def __init__(self):
        super(flat_trend_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("EMA Period", "EMA trend filter", "Indicators")

        self._prev_atr = 0.0
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(flat_trend_strategy, self).OnReseted()
        self._prev_atr = 0.0
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(flat_trend_strategy, self).OnStarted(time)

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
        return flat_trend_strategy()
