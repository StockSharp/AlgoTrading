import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class manual_trading_lightweight_utility_strategy(Strategy):
    def __init__(self):
        super(manual_trading_lightweight_utility_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._wma_period = self.Param("WmaPeriod", 50) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_wma = 0.0
        self._has_prev = False
        self._was_bullish = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(manual_trading_lightweight_utility_strategy, self).OnReseted()
        self._prev_wma = 0.0
        self._has_prev = False
        self._was_bullish = False

    def OnStarted(self, time):
        super(manual_trading_lightweight_utility_strategy, self).OnStarted(time)

        self._wma = WeightedMovingAverage()
        self._wma.Length = self.wma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._wma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return manual_trading_lightweight_utility_strategy()
