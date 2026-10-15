import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import AverageDirectionalIndex, DonchianChannels
from StockSharp.Algo.Strategies import Strategy

class adx_donchian_strategy(Strategy):
    """
    ADX Donchian strategy.
    The borders sit Multiplier percent inside the DonchianPeriod channel, which includes the current candle. With ADX above AdxThreshold
    a close at or above the upper border goes long and one at or below the lower border goes short, reversing an opposite position.
    The position closes once ADX falls below AdxThreshold minus 5, and a percent stop limits the loss.
    """

    def __init__(self):
        super(adx_donchian_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period of ADX", "Indicators")
        self._donchian_period = self.Param("DonchianPeriod", 5).SetGreaterThanZero().SetDisplay("Donchian Period", "Candles the channel spans", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 10).SetGreaterThanZero().SetDisplay("ADX Threshold", "ADX value for strong trend detection", "Indicators")
        self._multiplier = self.Param("Multiplier", 0.1).SetNotNegative().SetDisplay("Multiplier", "Percent the borders sit inside the channel", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(adx_donchian_strategy, self).OnStarted2(time)

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        donchian = DonchianChannels()
        donchian.Length = self._donchian_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, donchian, self._process_candle).Start()

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
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, adx_value, donchian_value):
        if candle.State != CandleStates.Finished:
            return

        if not adx_value.IsFormed or not donchian_value.IsFormed:
            return
        if adx_value.MovingAverage is None or donchian_value.UpperBand is None or donchian_value.LowerBand is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        strength = adx_value.MovingAverage
        threshold = Decimal(self._adx_threshold.Value)
        shift = Decimal(self._multiplier.Value) / Decimal(100)
        upper_border = donchian_value.UpperBand * (Decimal(1) - shift)
        lower_border = donchian_value.LowerBand * (Decimal(1) + shift)
        strong = strength > threshold
        weak = strength < threshold - Decimal(5)
        close = candle.ClosePrice

        if strong and close >= upper_border and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif strong and close <= lower_border and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and weak:
            self.SellMarket(self.Position)
        elif self.Position < 0 and weak:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return adx_donchian_strategy()
