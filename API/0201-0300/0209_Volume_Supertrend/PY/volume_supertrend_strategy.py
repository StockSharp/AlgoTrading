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

class volume_supertrend_strategy(Strategy):
    """
    Volume Supertrend strategy.
    A close above the Supertrend line on volume above the average of the previous VolumeAvgPeriod candles goes long and a close below it
    on such volume goes short, reversing an opposite position. A long closes once the Supertrend turns down and a short once it turns up,
    and a percent stop limits the loss.
    """

    def __init__(self):
        super(volume_supertrend_strategy, self).__init__()
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20).SetGreaterThanZero().SetDisplay("Volume Avg Period", "Previous candles the volume is averaged over", "Volume")
        self._supertrend_period = self.Param("SupertrendPeriod", 10).SetGreaterThanZero().SetDisplay("Supertrend Period", "ATR period of Supertrend", "Supertrend")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Supertrend")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []

    def OnReseted(self):
        super(volume_supertrend_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(volume_supertrend_strategy, self).OnStarted2(time)

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
        period = self._volume_avg_period.Value
        average = None
        if len(self._volumes) == period:
            total = Decimal(0)
            for volume in self._volumes:
                total += volume
            average = total / Decimal(period)

        self._volumes.append(candle.TotalVolume)
        if len(self._volumes) > period:
            self._volumes.pop(0)

        if not supertrend_value.IsFormed or average is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        line = supertrend_value.Value
        close = candle.ClosePrice
        surge = candle.TotalVolume > average

        if surge and close > line and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif surge and close < line and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and not supertrend_value.IsUpTrend:
            self.SellMarket(self.Position)
        elif self.Position < 0 and supertrend_value.IsUpTrend:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return volume_supertrend_strategy()
