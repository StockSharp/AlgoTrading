import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class gold_warrior02b_impulse_strategy(Strategy):
    def __init__(self):
        super(gold_warrior02b_impulse_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 21) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")

        self._prev_cci = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gold_warrior02b_impulse_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(gold_warrior02b_impulse_strategy, self).OnStarted(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._cci, self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return gold_warrior02b_impulse_strategy()
