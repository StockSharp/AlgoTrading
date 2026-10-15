import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTime, DateTimeKind
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class backtesting_module_strategy(Strategy):
    """
    Backtesting module strategy.
    Inside the StartTime..EndTime interval a fast SMA crossing above the slow SMA goes long and a cross below goes short,
    reversing an opposite position. Outside the interval no entries are taken and an open position is closed.
    """

    def __init__(self):
        super(backtesting_module_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Length", "Fast SMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Length", "Slow SMA period", "Indicators")
        self._start_time = self.Param("StartTime", DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc)) \
            .SetDisplay("Start Time", "Start of the trading interval", "Time")
        self._end_time = self.Param("EndTime", DateTime(2050, 12, 31, 0, 0, 0, DateTimeKind.Utc)) \
            .SetDisplay("End Time", "End of the trading interval", "Time")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_fast = None
        self._prev_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(backtesting_module_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(backtesting_module_strategy, self).OnStarted2(time)

        self._prev_fast = None
        self._prev_slow = None

        fast = SimpleMovingAverage()
        fast.Length = self._fast_length.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)
        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if prev_fast is None or prev_slow is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        open_time = candle.OpenTime
        in_window = open_time >= self._start_time.Value and open_time <= self._end_time.Value

        if not in_window:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
            return

        if prev_fast <= prev_slow and fast > slow and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev_fast >= prev_slow and fast < slow and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return backtesting_module_strategy()
