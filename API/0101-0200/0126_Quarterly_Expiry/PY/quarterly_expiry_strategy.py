import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, DateTime, DayOfWeek, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class quarterly_expiry_strategy(Strategy):
    """
    Quarterly Expiry strategy.
    Days are UTC days of a market that trades around the clock.
    Quarterly expiry is the third Friday of March, June, September and December. At the close of the first candle on the Monday of that
    week it trades in the direction of the trend (long above the MaPeriod SMA, short below) and closes at Thursday's last candle,
    before settlement; a percent stop limits the loss.
    """

    def __init__(self):
        super(quarterly_expiry_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "SMA that defines the trend", "Calendar")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._exit_day = None

    def OnReseted(self):
        super(quarterly_expiry_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(quarterly_expiry_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        first_of_day = self._day is None or self._day != day
        if first_of_day:
            self._day = day

        # The candle is the day's last when the next one would open on another day.
        frame = self.candle_type.Arg
        last_of_day = (candle.OpenTime + frame).Date != day
        ma = sma_value.GetValue[Decimal](None) if sma_value.IsFormed else None

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            if last_of_day and day == self._exit_day:
                if self.Position > 0:
                    self.SellMarket(self.Position)
                else:
                    self.BuyMarket(-self.Position)
            return

        if not (first_of_day and ma is not None and candle.ClosePrice != ma and day.Month % 3 == 0 and day == self._third_friday(day).AddDays(-4)):
            return

        if (1 if candle.ClosePrice > ma else -1) > 0:
            self.BuyMarket(self.Volume)
        else:
            self.SellMarket(self.Volume)
        self._exit_day = self._third_friday(day).AddDays(-1)

    @staticmethod
    def _third_friday(day):
        first = DateTime(day.Year, day.Month, 1, 0, 0, 0, day.Kind)
        return first.AddDays(((int(DayOfWeek.Friday) - int(first.DayOfWeek) + 7) % 7) + 14)

    def CreateClone(self):
        return quarterly_expiry_strategy()
