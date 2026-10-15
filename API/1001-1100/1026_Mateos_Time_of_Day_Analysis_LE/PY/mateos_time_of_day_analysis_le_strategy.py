import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class mateos_time_of_day_analysis_le_strategy(Strategy):
    """
    Mateo's Time of Day Analysis LE strategy.
    Between the From and Thru dates a long position is opened once the candle time reaches StartTime and closed once it reaches
    EndTime. Times are the candle open time in UTC.
    """

    def __init__(self):
        super(mateos_time_of_day_analysis_le_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._start_time = self.Param("StartTime", TimeSpan(9, 30, 0)).SetDisplay("Start Time", "Time of day the long position opens", "Time")
        self._end_time = self.Param("EndTime", TimeSpan(16, 0, 0)).SetDisplay("End Time", "Time of day the long position closes", "Time")
        self._from = self.Param("From", DateTimeOffset(2017, 4, 21, 0, 0, 0, TimeSpan.Zero)).SetDisplay("From", "First date entries are allowed", "Time")
        self._thru = self.Param("Thru", DateTimeOffset(2099, 12, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Thru", "Last date entries are allowed", "Time")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(mateos_time_of_day_analysis_le_strategy, self).OnStarted2(time)

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
        time_of_day = open_time.TimeOfDay
        in_session = time_of_day >= self._start_time.Value and time_of_day < self._end_time.Value
        in_date_range = open_time >= self._from.Value.UtcDateTime and open_time <= self._thru.Value.UtcDateTime

        if in_session and in_date_range and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif not in_session and self.Position > 0:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return mateos_time_of_day_analysis_le_strategy()
