import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy

class supertrend_volume_strategy(Strategy):
    """
    Supertrend Volume strategy.
    A Supertrend flip happens when a candle closes on the other side of the line. A position closes on the flip against it,
    and the flip opens a position in its direction only when the candle's volume is above the average of the previous VolumePeriod candles,
    so a confirmed flip reverses the position. A percent stop limits the loss.
    """

    def __init__(self):
        super(supertrend_volume_strategy, self).__init__()
        self._supertrend_period = self.Param("SupertrendPeriod", 10).SetGreaterThanZero().SetDisplay("Supertrend Period", "ATR period of Supertrend", "Supertrend")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Supertrend")
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Volume")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []
        self._prev_is_up_trend = None

    def OnReseted(self):
        super(supertrend_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(supertrend_volume_strategy, self).OnStarted2(time)

        self._reset_state()

        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_period.Value
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(supertrend, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, supertrend_value):
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

        if not supertrend_value.IsFormed:
            return

        is_up_trend = supertrend_value.IsUpTrend
        was_up_trend = self._prev_is_up_trend
        self._prev_is_up_trend = is_up_trend

        if was_up_trend is None or was_up_trend == is_up_trend:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        confirmed = average is not None and candle.TotalVolume > average

        if is_up_trend:
            if confirmed and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
        else:
            if confirmed and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return supertrend_volume_strategy()
