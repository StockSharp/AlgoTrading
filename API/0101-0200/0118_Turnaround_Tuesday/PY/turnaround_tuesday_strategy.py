import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, DateTime, DayOfWeek, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class turnaround_tuesday_strategy(Strategy):
    """
    Turnaround Tuesday strategy.
    Days are UTC days of a market that trades around the clock.
    When Monday closed below its open, it buys at the close of Tuesday's first candle and sells at Tuesday's last candle
    or once the close reaches ProfitTargetPercent above the entry; a percent stop limits the loss.
    """

    def __init__(self):
        super(turnaround_tuesday_strategy, self).__init__()
        self._profit_target_percent = self.Param("ProfitTargetPercent", 1.0).SetGreaterThanZero().SetDisplay("Profit Target %", "Close once the price is this far above the entry, in percent", "Calendar")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._prev_day = None
        self._prev_day_open = Decimal(0)
        self._prev_day_close = Decimal(0)
        self._entry_price = Decimal(0)
        self._day_open = Decimal(0)
        self._day_close = Decimal(0)

    def OnReseted(self):
        super(turnaround_tuesday_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(turnaround_tuesday_strategy, self).OnStarted2(time)

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

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        first_of_day = self._day is None or self._day != day
        if first_of_day:
            if self._day is not None:
                self._prev_day = self._day
                self._prev_day_open = self._day_open
                self._prev_day_close = self._day_close
            self._day = day
            self._day_open = candle.OpenPrice
        self._day_close = candle.ClosePrice

        # The candle is the day's last when the next one would open on another day.
        frame = self.candle_type.Arg
        last_of_day = (candle.OpenTime + frame).Date != day

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            if last_of_day or candle.ClosePrice >= self._entry_price * (Decimal(1) + Decimal(self._profit_target_percent.Value) / Decimal(100)):
                if self.Position > 0:
                    self.SellMarket(self.Position)
                else:
                    self.BuyMarket(-self.Position)
            return

        if not (first_of_day and day.DayOfWeek == DayOfWeek.Tuesday and self._prev_day is not None and self._prev_day == day.AddDays(-1) and self._prev_day_close < self._prev_day_open):
            return

        if (1) > 0:
            self.BuyMarket(self.Volume)
        else:
            self.SellMarket(self.Volume)
        self._entry_price = candle.ClosePrice

    def CreateClone(self):
        return turnaround_tuesday_strategy()
