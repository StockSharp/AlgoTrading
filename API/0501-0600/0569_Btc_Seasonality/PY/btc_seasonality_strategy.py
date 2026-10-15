import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DayOfWeek
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

EST_OFFSET = TimeSpan.FromHours(-5)


class btc_seasonality_strategy(Strategy):
    """
    BTC Seasonality strategy.
    Opens a long (IsLong) or short position at the first candle of EntryHour on EntryDay and closes it at the first candle of
    ExitHour on ExitDay. Days and hours are in Eastern Standard Time (UTC-5).
    """

    def __init__(self):
        super(btc_seasonality_strategy, self).__init__()
        self._entry_day = self.Param("EntryDay", DayOfWeek.Saturday).SetDisplay("Entry Day", "EST day of the entry", "Schedule")
        self._exit_day = self.Param("ExitDay", DayOfWeek.Monday).SetDisplay("Exit Day", "EST day of the exit", "Schedule")
        self._entry_hour = self.Param("EntryHour", 10).SetRange(0, 23).SetDisplay("Entry Hour", "EST hour of the entry", "Schedule")
        self._exit_hour = self.Param("ExitHour", 10).SetRange(0, 23).SetDisplay("Exit Hour", "EST hour of the exit", "Schedule")
        self._is_long = self.Param("IsLong", True).SetDisplay("Is Long", "Trade long when true, short otherwise", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(btc_seasonality_strategy, self).OnStarted2(time)

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

        est = candle.OpenTime.Add(EST_OFFSET)
        is_exit_moment = est.DayOfWeek == self._exit_day.Value and est.Hour == self._exit_hour.Value

        if self.Position != 0:
            if is_exit_moment:
                if self.Position > 0:
                    self.SellMarket(self.Position)
                else:
                    self.BuyMarket(-self.Position)
            return

        if est.DayOfWeek != self._entry_day.Value or est.Hour != self._entry_hour.Value:
            return

        # Avoid reopening right after the exit when entry and exit share the same moment.
        if is_exit_moment:
            return

        if self._is_long.Value:
            self.BuyMarket(self.Volume)
        else:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return btc_seasonality_strategy()
