import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy

class donchian_volume_strategy(Strategy):
    """
    Donchian Volume strategy.
    The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close above it on volume above
    the average of the previous VolumePeriod candles goes long, a close below it on such volume goes short, reversing an opposite position.
    The position closes once price closes back inside the channel or volume falls below its average, and a percent stop limits the loss.
    """

    def __init__(self):
        super(donchian_volume_strategy, self).__init__()
        self._donchian_period = self.Param("DonchianPeriod", 20).SetGreaterThanZero().SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators")
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []
        self._prev_upper = None
        self._prev_lower = None

    def OnReseted(self):
        super(donchian_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(donchian_volume_strategy, self).OnStarted2(time)

        self._reset_state()

        donchian = DonchianChannels()
        donchian.Length = self._donchian_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, donchian_value):
        if candle.State != CandleStates.Finished:
            return

        # Volume and the channel are measured on the candles before this one.
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

        upper = self._prev_upper
        lower = self._prev_lower

        if donchian_value.IsFormed and donchian_value.UpperBand is not None and donchian_value.LowerBand is not None:
            self._prev_upper = donchian_value.UpperBand
            self._prev_lower = donchian_value.LowerBand

        if average is None or upper is None or lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        volume = candle.TotalVolume
        signal = 0
        if volume > average:
            if close > upper:
                signal = 1
            elif close < lower:
                signal = -1

        if signal > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif signal < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and (close <= upper or volume < average):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close >= lower or volume < average):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return donchian_volume_strategy()
