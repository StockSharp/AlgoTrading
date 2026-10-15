import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_volume_strategy(Strategy):
    """
    MA Volume strategy.
    Volume expands when a candle's volume exceeds VolumeThreshold times the average of the previous VolumePeriod candles.
    While flat, an expanding candle that closes above a rising MaPeriod SMA goes long and one that closes below a falling SMA goes short.
    The position closes once volume dries up below its average or the SMA turns against it, and a percent stop limits the loss.
    """

    def __init__(self):
        super(ma_volume_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period of the trend SMA", "Indicators")
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Indicators")
        self._volume_threshold = self.Param("VolumeThreshold", 1.2).SetGreaterThanZero().SetDisplay("Volume Threshold", "How many times the average volume an expanding candle must exceed", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []
        self._prev_ma = None

    def OnReseted(self):
        super(ma_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ma_volume_strategy, self).OnStarted2(time)

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

        # Expansion is measured against the candles before this one.
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

        if not sma_value.IsFormed:
            return

        ma = sma_value.GetValue[Decimal](None)
        prev_ma = self._prev_ma
        self._prev_ma = ma

        if average is None or prev_ma is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        volume = candle.TotalVolume

        if self.Position > 0:
            if volume < average or ma < prev_ma:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if volume < average or ma > prev_ma:
                self.BuyMarket(-self.Position)
        elif volume > average * Decimal(self._volume_threshold.Value):
            if close > ma and ma > prev_ma:
                self.BuyMarket(self.Volume)
            elif close < ma and ma < prev_ma:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return ma_volume_strategy()
