import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class molly_etf_ema_crossover_strategy(Strategy):
    """
    Molly ETF EMA crossover: long when fast EMA crosses above slow, exit on opposite cross.
    """

    def __init__(self):
        super(molly_etf_ema_crossover_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10).SetDisplay("Fast EMA", "Fast EMA length", "Parameters")
        self._slow_length = self.Param("SlowLength", 21).SetDisplay("Slow EMA", "Slow EMA length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candles", "General")

        self._was_fast_above = False
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(molly_etf_ema_crossover_strategy, self).OnReseted()
        self._was_fast_above = False
        self._initialized = False

    def OnStarted(self, time):
        super(molly_etf_ema_crossover_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        if not self._initialized:
            self._was_fast_above = fast > slow
            self._initialized = True
            return
        is_fast_above = fast > slow
        cross_over = not self._was_fast_above and is_fast_above
        cross_under = self._was_fast_above and not is_fast_above
        if cross_over and self.Position <= 0:
            self.BuyMarket()
        if cross_under and self.Position > 0:
            self.SellMarket()
        self._was_fast_above = is_fast_above

    def CreateClone(self):
        return molly_etf_ema_crossover_strategy()
