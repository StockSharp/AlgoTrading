import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DayOfWeek
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class monday_open_strategy(Strategy):
    """
    Monday open strategy.
    Buys at the beginning of the week on Monday and closes the long position at Tuesday's close,
    trading only in years from StartYear to EndYear.
    """

    def __init__(self):
        super(monday_open_strategy, self).__init__()
        self._start_year = self.Param("StartYear", 2023).SetDisplay("Start Year", "First year in which trading is allowed", "General")
        self._end_year = self.Param("EndYear", 2025).SetDisplay("End Year", "Last year in which trading is allowed", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._last_entry_date = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(monday_open_strategy, self).OnReseted()
        self._last_entry_date = None

    def OnStarted2(self, time):
        super(monday_open_strategy, self).OnStarted2(time)

        self._last_entry_date = None

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        open_time = candle.OpenTime
        frame = self.candle_type.Arg
        close_time = open_time + frame if isinstance(frame, TimeSpan) else open_time
        day = open_time.DayOfWeek

        if self.Position > 0:
            # Exit on the candle that completes Tuesday, or on the first later candle if that one was missing.
            tuesday_closed = day == DayOfWeek.Tuesday and close_time.Date > open_time.Date
            if tuesday_closed or (day != DayOfWeek.Monday and day != DayOfWeek.Tuesday):
                self.SellMarket(self.Position)
            return

        year = open_time.Year
        if year < self._start_year.Value or year > self._end_year.Value:
            return

        if day == DayOfWeek.Monday and self._last_entry_date != open_time.Date and self.Position == 0:
            self.BuyMarket(self.Volume)
            self._last_entry_date = open_time.Date

    def CreateClone(self):
        return monday_open_strategy()
