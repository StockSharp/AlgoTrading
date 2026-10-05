import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class how_to_set_backtest_time_ranges_strategy(Strategy):
    """
    How To Set Backtest Time Ranges strategy.
    Goes long when the fast SMA crosses above the slow SMA and closes the long when it crosses below. Signals are taken only
    between FromDate and ThruDate; entries additionally require the candle time of day to be inside EntryStart..EntryEnd and
    exits inside ExitStart..ExitEnd. Equal window bounds mean the whole day, and a window may wrap past midnight.
    """

    def __init__(self):
        super(how_to_set_backtest_time_ranges_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 14).SetGreaterThanZero().SetDisplay("Fast Length", "Fast SMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 28).SetGreaterThanZero().SetDisplay("Slow Length", "Slow SMA length", "Indicators")
        self._from_date = self.Param("FromDate", DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("From Date", "First date of the trading range", "Time")
        self._thru_date = self.Param("ThruDate", DateTimeOffset(2112, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Thru Date", "Last date of the trading range", "Time")
        self._entry_start = self.Param("EntryStart", TimeSpan.Zero).SetDisplay("Entry Start", "Start of the entry time window", "Time")
        self._entry_end = self.Param("EntryEnd", TimeSpan.Zero).SetDisplay("Entry End", "End of the entry time window", "Time")
        self._exit_start = self.Param("ExitStart", TimeSpan.Zero).SetDisplay("Exit Start", "Start of the exit time window", "Time")
        self._exit_end = self.Param("ExitEnd", TimeSpan.Zero).SetDisplay("Exit End", "End of the exit time window", "Time")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_fast = None
        self._prev_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(how_to_set_backtest_time_ranges_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(how_to_set_backtest_time_ranges_strategy, self).OnStarted2(time)

        self._prev_fast = None
        self._prev_slow = None

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self._fast_length.Value
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self._slow_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_sma, slow_sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    @staticmethod
    def _in_window(time, start, end):
        if start == end:
            return True
        if start < end:
            return time >= start and time < end
        return time >= start or time < end

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
        if open_time < self._from_date.Value.UtcDateTime or open_time > self._thru_date.Value.UtcDateTime:
            return

        time_of_day = open_time.TimeOfDay

        if prev_fast <= prev_slow and fast > slow and self.Position <= 0 and self._in_window(time_of_day, self._entry_start.Value, self._entry_end.Value):
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev_fast >= prev_slow and fast < slow and self.Position > 0 and self._in_window(time_of_day, self._exit_start.Value, self._exit_end.Value):
            self.SellMarket(self.Position)

    def CreateClone(self):
        return how_to_set_backtest_time_ranges_strategy()
