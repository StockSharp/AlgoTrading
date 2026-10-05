import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class lunch_break_fade_strategy(Strategy):
    """
    Lunch Break Fade strategy.
    The market trades around the clock, so the session is the UTC day.
    The morning move runs from the UTC day's open to the close at LunchHour. At that close the strategy enters against the move
    and covers at the candle ending at LunchEndHour, before volume returns; a percent stop limits the loss.
    """

    def __init__(self):
        super(lunch_break_fade_strategy, self).__init__()
        self._lunch_hour = self.Param("LunchHour", 12).SetRange(0, 23).SetDisplay("Lunch Hour", "UTC hour that ends the morning", "Session")
        self._lunch_end_hour = self.Param("LunchEndHour", 14).SetRange(0, 23).SetDisplay("Lunch End Hour", "UTC hour at which the position is covered", "Session")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._day_open = Decimal(0)

    def OnReseted(self):
        super(lunch_break_fade_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(lunch_break_fade_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(-self.Position)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        first_of_day = self._day is None or self._day != day
        if first_of_day:
            self._day = day
            self._day_open = candle.OpenPrice

        frame = self.candle_type.Arg
        close_time = candle.OpenTime + frame

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            if close_time >= day.AddHours(self._lunch_end_hour.Value):
                self._close_position()
            return

        if close_time != day.AddHours(self._lunch_hour.Value):
            return

        if candle.ClosePrice > self._day_open:
            self.SellMarket(self.Volume)
        elif candle.ClosePrice < self._day_open:
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return lunch_break_fade_strategy()
