import clr

clr.AddReference("Ecng.ComponentModel")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, TimeSpan, TimeZoneInfo
from Ecng.ComponentModel import WorkingTimeExtensions
from StockSharp.Messages import DataType, CandleStates, OrderStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy


class santa_claus_rally_strategy(Strategy):
    """Long-only rally over the last five December sessions and first two January sessions."""

    def __init__(self):
        super(santa_claus_rally_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay(
            "Candle Type", "Finished candles used for calendar decisions", "Data")
        self._stop_loss = self.Param("StopLoss", 2.0).SetGreaterThanZero().SetDisplay(
            "Stop Loss %", "Percentage loss limit from the executed entry", "Risk")
        self._entry_year = -1
        self._pending_order = None
        self.OrderRegistering += self._on_order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _on_order(self, order):
        self._pending_order = order

    def OnReseted(self):
        super(santa_claus_rally_strategy, self).OnReseted()
        self._entry_year = -1
        self._pending_order = None

    def OnStarted2(self, time):
        if self.Security is None or self.Security.Board is None:
            raise ValueError("The instrument's trading board and calendar are required")
        super(santa_claus_rally_strategy, self).OnStarted2(time)
        self._entry_year = -1
        self._pending_order = None
        self.StartProtection(Unit(), Unit(float(self._stop_loss.Value), UnitTypes.Percent),
                             useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Start()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _season(self, year):
        calendar = self.Security.Board.WorkingTime
        entry = DateTime(year, 12, 31)
        sessions = 0
        while True:
            if WorkingTimeExtensions.IsWorkingDate(calendar, entry, True):
                sessions += 1
                if sessions == 5:
                    break
            entry = entry.AddDays(-1)
        exit_date = DateTime(year + 1, 1, 1)
        sessions = 0
        while True:
            if WorkingTimeExtensions.IsWorkingDate(calendar, exit_date, True):
                sessions += 1
                if sessions == 2:
                    break
            exit_date = exit_date.AddDays(1)
        return entry, exit_date

    def _session_end(self, date):
        calendar = self.Security.Board.WorkingTime
        if not calendar.IsEnabled:
            return TimeSpan.FromDays(1)
        period = WorkingTimeExtensions.GetPeriod(calendar, date)
        sessions = list(period.Times) if period is not None else []
        if calendar.SpecialDays.ContainsKey(date):
            sessions = list(calendar.SpecialDays[date])
        elif period is not None and period.SpecialDays.ContainsKey(date.DayOfWeek):
            sessions = list(period.SpecialDays[date.DayOfWeek])
        if not sessions:
            return TimeSpan.FromDays(1)
        end = sessions[0].Max
        for session in sessions[1:]:
            if session.Max > end:
                end = session.Max
        return end

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        local = TimeZoneInfo.ConvertTimeFromUtc(candle.OpenTime, self.Security.Board.TimeZone)
        date = local.Date
        year = local.Year if local.Month == 12 else local.Year - 1
        entry, exit_date = self._season(year)
        if self.Position > 0:
            bar_end = TimeZoneInfo.ConvertTimeFromUtc(candle.OpenTime.Add(self.candle_type.Arg), self.Security.Board.TimeZone)
            if date > exit_date or (date == exit_date and bar_end >= exit_date.Add(self._session_end(exit_date))):
                self.SellMarket(self.Position)
        elif self.Position == 0 and local.Month == 12 and date >= entry and self._entry_year != year:
            if WorkingTimeExtensions.IsWorkingDate(self.Security.Board.WorkingTime, date, True):
                self._entry_year = year
                self.BuyMarket(self.Volume)

    def CreateClone(self):
        return santa_claus_rally_strategy()