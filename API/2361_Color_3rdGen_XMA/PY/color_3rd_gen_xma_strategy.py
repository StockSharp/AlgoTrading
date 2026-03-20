import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_3rd_gen_xma_strategy(Strategy):
    def __init__(self):
        super(color_3rd_gen_xma_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 20) \
            .SetDisplay("Fast EMA", "Fast EMA period", "General")
        self._slow_length = self.Param("SlowLength", 50) \
            .SetDisplay("Slow EMA", "Slow EMA period", "General")
        self._min_spread = self.Param("MinSpread", 20.0) \
            .SetDisplay("Min Spread", "Minimum EMA spread in price steps", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def min_spread(self):
        return self._min_spread.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_3rd_gen_xma_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    def OnStarted(self, time):
        super(color_3rd_gen_xma_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = int(self.fast_length)
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = int(self.slow_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        if self._is_first:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_first = False
            return
        spread = abs(fast - slow)
        ms = float(self.min_spread)
        price_step = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 1.0
        if spread < ms * price_step:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        prev_above = self._prev_fast > self._prev_slow
        cur_above = fast > slow
        if not prev_above and cur_above and self.Position <= 0:
            self.BuyMarket()
        elif prev_above and not cur_above and self.Position >= 0:
            self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return color_3rd_gen_xma_strategy()
