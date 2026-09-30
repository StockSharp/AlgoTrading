import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTimeOffset, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class buy_and_hold_strategy(Strategy):
    """Buys once at the start date and holds the long until the end date."""

    def __init__(self):
        super(buy_and_hold_strategy, self).__init__()
        self._start_date = self.Param("StartDate", DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero)) \
            .SetDisplay("Start Date", "Buy once on or after this date", "General")
        self._end_date = self.Param("EndDate", DateTimeOffset(2069, 12, 31, 0, 0, 0, TimeSpan.Zero)) \
            .SetDisplay("End Date", "Close the long on or after this date", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entry_submitted = False
        self._exit_submitted = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(buy_and_hold_strategy, self).OnReseted()
        self._entry_submitted = False
        self._exit_submitted = False

    def OnStarted2(self, time):
        super(buy_and_hold_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if candle.OpenTime >= self._end_date.Value.UtcDateTime:
            if not self._exit_submitted and self.Position > 0:
                self.SellMarket(self.Position)
                self._exit_submitted = True
        elif (not self._entry_submitted and candle.OpenTime >= self._start_date.Value.UtcDateTime
                and self.Position == 0):
            self.BuyMarket()
            # Remember the submitted entry even while its fill is still pending.
            self._entry_submitted = True

    def CreateClone(self):
        return buy_and_hold_strategy()
