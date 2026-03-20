import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy


class close_profit_end_of_week_strategy(Strategy):
    def __init__(self):
        super(close_profit_end_of_week_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._mom_period = self.Param("MomPeriod", 20) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._momentum_level = self.Param("MomentumLevel", 101) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_mom = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(close_profit_end_of_week_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(close_profit_end_of_week_strategy, self).OnStarted(time)

        self._mom = Momentum()
        self._mom.Length = self.mom_period
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._mom, self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return close_profit_end_of_week_strategy()
