import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, DateTime, DayOfWeek, Decimal
from System.Globalization import CultureInfo
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class pre_holiday_strength_strategy(Strategy):
    """
    Pre-Holiday Strength strategy.
    Days are UTC days of a market that trades around the clock.
    Holidays is a comma-separated list of yyyy-MM-dd dates. The market trades every day, so it buys at the close of the first candle
    of the calendar day before a holiday and sells at that day's last candle; a percent stop limits the loss.
    """

    def __init__(self):
        super(pre_holiday_strength_strategy, self).__init__()
        self._holidays_text = self.Param("Holidays", "2024-01-01,2024-01-15,2024-02-19,2024-03-29,2024-05-27,2024-06-19,2024-07-04,2024-09-02,2024-11-28,2024-12-25").SetDisplay("Holidays", "Comma-separated yyyy-MM-dd holiday dates", "Calendar")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._holidays = set()

    def OnReseted(self):
        super(pre_holiday_strength_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(pre_holiday_strength_strategy, self).OnStarted2(time)

        self._reset_state()
        self._holidays = set(DateTime.ParseExact(item.strip(), "yyyy-MM-dd", CultureInfo.InvariantCulture) for item in str(self._holidays_text.Value).split(",") if item.strip())

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
            self._day = day

        # The candle is the day's last when the next one would open on another day.
        frame = self.candle_type.Arg
        last_of_day = (candle.OpenTime + frame).Date != day

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            if last_of_day:
                if self.Position > 0:
                    self.SellMarket(self.Position)
                else:
                    self.BuyMarket(-self.Position)
            return

        if not (first_of_day and day.AddDays(1) in self._holidays):
            return

        if (1) > 0:
            self.BuyMarket(self.Volume)
        else:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return pre_holiday_strength_strategy()
