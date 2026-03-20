import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class manual_trading_lightweight_utility_strategy(Strategy):
    def __init__(self):
        super(manual_trading_lightweight_utility_strategy, self).__init__()

        self._wma_period = self.Param("WmaPeriod", 50) \
            .SetDisplay("WMA Period", "Weighted MA period", "Indicators")

        self._wma = None
        self._prev_wma = 0.0
        self._has_prev = False
        self._was_bullish = False

    @property
    def wma_period(self):
        return self._wma_period.Value

    def OnReseted(self):
        super(manual_trading_lightweight_utility_strategy, self).OnReseted()
        self._wma = None
        self._prev_wma = 0.0
        self._has_prev = False
        self._was_bullish = False

    def OnStarted(self, time):
        super(manual_trading_lightweight_utility_strategy, self).OnStarted(time)

        self._wma = WeightedMovingAverage()
        self._wma.Length = self.wma_period
        self._has_prev = False

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._wma, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, wma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._wma.IsFormed:
            return

        close = float(candle.ClosePrice)
        wma_val = float(wma_value)
        is_bullish = close > wma_val and wma_val > self._prev_wma

        if self._has_prev:
            if is_bullish and not self._was_bullish and self.Position <= 0:
                self.BuyMarket()
            elif not is_bullish and close < wma_val and wma_val < self._prev_wma and self._was_bullish and self.Position >= 0:
                self.SellMarket()

        self._prev_wma = wma_val
        self._has_prev = True
        self._was_bullish = is_bullish

    def CreateClone(self):
        return manual_trading_lightweight_utility_strategy()
