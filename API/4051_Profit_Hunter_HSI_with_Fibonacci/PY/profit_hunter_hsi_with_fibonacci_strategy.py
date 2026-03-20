import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class profit_hunter_hsi_with_fibonacci_strategy(Strategy):
    def __init__(self):
        super(profit_hunter_hsi_with_fibonacci_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Period for trend filter EMA.", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 50) \
            .SetDisplay("EMA Period", "Period for trend filter EMA.", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("EMA Period", "Period for trend filter EMA.", "Indicators")

        self._bar_count = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(profit_hunter_hsi_with_fibonacci_strategy, self).OnReseted()
        self._bar_count = 0.0

    def OnStarted(self, time):
        super(profit_hunter_hsi_with_fibonacci_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._highest = Highest()
        self._highest.Length = self.lookback_period
        self._lowest = Lowest()
        self._lowest.Length = self.lookback_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._highest, self._lowest, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return profit_hunter_hsi_with_fibonacci_strategy()
