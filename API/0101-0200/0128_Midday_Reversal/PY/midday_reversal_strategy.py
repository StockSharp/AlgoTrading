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

class midday_reversal_strategy(Strategy):
    """
    Midday Reversal strategy.
    The market trades around the clock, so the session is the UTC day.
    The morning move runs from the UTC day's open to the close at MiddayHour. From then until AfternoonHour, the first candle that closes
    against that move opens a position against it, once a day. The position closes at the candle ending at AfternoonHour,
    and a percent stop limits the loss.
    """

    def __init__(self):
        super(midday_reversal_strategy, self).__init__()
        self._midday_hour = self.Param("MiddayHour", 12).SetRange(0, 23).SetDisplay("Midday Hour", "UTC hour that ends the morning", "Session")
        self._afternoon_hour = self.Param("AfternoonHour", 16).SetRange(0, 23).SetDisplay("Afternoon Hour", "UTC hour by which the reversal must have worked", "Session")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._day_open = Decimal(0)
        self._morning = 0
        self._traded_today = False

    def OnReseted(self):
        super(midday_reversal_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(midday_reversal_strategy, self).OnStarted2(time)

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
            self._morning = 0
            self._traded_today = False

        frame = self.candle_type.Arg
        close_time = candle.OpenTime + frame
        midday = day.AddHours(self._midday_hour.Value)
        afternoon = day.AddHours(self._afternoon_hour.Value)

        if close_time == midday:
            self._morning = 1 if candle.ClosePrice > self._day_open else (-1 if candle.ClosePrice < self._day_open else 0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            if close_time >= afternoon:
                self._close_position()
            return

        window = close_time > midday and close_time < afternoon
        if not window or self._traded_today or self._morning == 0:
            return

        direction = 1 if candle.ClosePrice > candle.OpenPrice else (-1 if candle.ClosePrice < candle.OpenPrice else 0)
        if direction == -self._morning:
            if self._morning > 0:
                self.SellMarket(self.Volume)
            else:
                self.BuyMarket(self.Volume)
            self._traded_today = True

    def CreateClone(self):
        return midday_reversal_strategy()
