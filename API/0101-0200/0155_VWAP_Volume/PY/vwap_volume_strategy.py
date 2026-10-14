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

class vwap_volume_strategy(Strategy):
    """
    VWAP Volume strategy.
    The market trades around the clock, so the session VWAP restarts with each UTC day and weighs each candle's typical price by its volume.
    A close below VWAP on volume above VolumeThreshold times the average of the previous VolumePeriod candles goes long and a close above
    VWAP on such volume goes short, reversing an opposite position. The position closes once price crosses back through VWAP,
    and a percent stop limits the loss.
    """

    def __init__(self):
        super(vwap_volume_strategy, self).__init__()
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Volume")
        self._volume_threshold = self.Param("VolumeThreshold", 1.5).SetGreaterThanZero().SetDisplay("Volume Threshold", "How many times the average volume a candle must exceed", "Volume")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []
        self._day = None
        self._cumulative_price_volume = Decimal(0)
        self._cumulative_volume = Decimal(0)

    def OnReseted(self):
        super(vwap_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(vwap_volume_strategy, self).OnStarted2(time)

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

        # Volume is compared with the candles before this one.
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

        day = candle.OpenTime.Date
        if self._day is None or self._day != day:
            self._day = day
            self._cumulative_price_volume = Decimal(0)
            self._cumulative_volume = Decimal(0)

        typical_price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3)
        self._cumulative_price_volume += typical_price * candle.TotalVolume
        self._cumulative_volume += candle.TotalVolume

        if average is None or self._cumulative_volume <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        vwap = self._cumulative_price_volume / self._cumulative_volume
        close = candle.ClosePrice
        surge = candle.TotalVolume > average * Decimal(self._volume_threshold.Value)

        if close < vwap and surge and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close > vwap and surge and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close > vwap:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close < vwap:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return vwap_volume_strategy()
