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

class open_drive_strategy(Strategy):
    """
    Open Drive strategy.
    The market trades around the clock, so the session is the UTC day.
    The first candle of each UTC day is the opening drive when its volume exceeds the average of the previous VolumePeriod candles;
    the strategy joins its direction at its close. The position closes on the first candle that closes against it, when the drive stalls,
    and a trailing percent stop follows it as price extends.
    """

    def __init__(self):
        super(open_drive_strategy, self).__init__()
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Previous candles the opening volume is compared with", "Session")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Trailing stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._volumes = []

    def OnReseted(self):
        super(open_drive_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(open_drive_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), isStopTrailing=True, useMarketOrders=True, isLocalStop=True)

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

        period = self._volume_period.Value
        average = None
        if len(self._volumes) == period:
            total = Decimal(0)
            for volume in self._volumes:
                total += volume
            average = total / Decimal(period)

        self._volumes.append(candle.TotalVolume)
        if len(self._volumes) > period:
            self._volumes.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        if self.Position > 0:
            if close < candle.OpenPrice:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if close > candle.OpenPrice:
                self.BuyMarket(-self.Position)
        elif first_of_day and average is not None and candle.TotalVolume > average:
            if close > candle.OpenPrice:
                self.BuyMarket(self.Volume)
            elif close < candle.OpenPrice:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return open_drive_strategy()
