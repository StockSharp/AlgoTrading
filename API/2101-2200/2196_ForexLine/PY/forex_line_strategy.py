import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import process_float


class forex_line_strategy(Strategy):
    """Cascaded 5/10 fast and 20/20 slow WMA strategy, matching the documented ForexLine."""

    def __init__(self):
        super(forex_line_strategy, self).__init__()
        self._fast_length1 = self.Param("FastLength1", 5).SetDisplay("Fast WMA 1", "First fast smoothing", "Indicator")
        self._fast_length2 = self.Param("FastLength2", 10).SetDisplay("Fast WMA 2", "Second fast smoothing", "Indicator")
        self._slow_length1 = self.Param("SlowLength1", 20).SetDisplay("Slow WMA 1", "First slow smoothing", "Indicator")
        self._slow_length2 = self.Param("SlowLength2", 20).SetDisplay("Slow WMA 2", "Second slow smoothing", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))).SetDisplay(
            "Candle Type", "Timeframe for price closes", "General")
        self._reset_state()

    @property
    def fast_length1(self): return self._fast_length1.Value
    @property
    def fast_length2(self): return self._fast_length2.Value
    @property
    def slow_length1(self): return self._slow_length1.Value
    @property
    def slow_length2(self): return self._slow_length2.Value
    @property
    def candle_type(self): return self._candle_type.Value

    def _reset_state(self):
        self._previous_fast = None
        self._previous_slow = None
        self._pending_order = None
        self._fast_first = self._fast_second = None
        self._slow_first = self._slow_second = None

    def OnReseted(self):
        super(forex_line_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(forex_line_strategy, self).OnStarted2(time)
        self._reset_state()
        self._fast_first = WeightedMovingAverage()
        self._fast_first.Length = self.fast_length1
        self._fast_second = WeightedMovingAverage()
        self._fast_second.Length = self.fast_length2
        self._slow_first = WeightedMovingAverage()
        self._slow_first.Length = self.slow_length1
        self._slow_second = WeightedMovingAverage()
        self._slow_second.Length = self.slow_length2
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        time = candle.OpenTime
        fast_first = process_float(self._fast_first, candle.ClosePrice, time, True)
        slow_first = process_float(self._slow_first, candle.ClosePrice, time, True)
        if not fast_first.IsFormed or not slow_first.IsFormed:
            return
        fast_second = process_float(self._fast_second, float(fast_first), time, True)
        slow_second = process_float(self._slow_second, float(slow_first), time, True)
        if not fast_second.IsFormed or not slow_second.IsFormed:
            return
        fast = float(fast_second)
        slow = float(slow_second)
        pf = self._previous_fast
        ps = self._previous_slow
        self._previous_fast = fast
        self._previous_slow = slow
        if pf is None or ps is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if pf <= ps and fast > slow and self.Position <= 0:
            self._pending_order = self.BuyMarket(self.Volume + abs(self.Position))
        elif pf >= ps and fast < slow and self.Position >= 0:
            self._pending_order = self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return forex_line_strategy()
