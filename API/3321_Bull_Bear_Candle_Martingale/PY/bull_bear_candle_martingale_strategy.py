import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bull_bear_candle_martingale_strategy(Strategy):
    def __init__(self):
        super(bull_bear_candle_martingale_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ema_period = self.Param("EmaPeriod", 30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bull_bear_candle_martingale_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(bull_bear_candle_martingale_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bull_bear_candle_martingale_strategy()
